using Plugins.View;

using System.Collections.Generic;
using System.Windows;
using System.Text;
using System.IO;
using System;

using AApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

using Newtonsoft.Json.Linq;

using static Plugins.Constants;

namespace Plugins
{
    public partial class Commands : IExtensionApplication
    {
        #region Private Methods

        /// <summary>
        /// Проверка загрузки всех необходимый стилей линий MapManager
        /// </summary>
        /// <param name="db">Внутренняя БД AutoCAD</param>
        /// <param name="types">Список необходимых стилей</param>
        /// <returns>true, если все стили корректно загружены, false в противном случае</returns>
        private bool VerifyLoadingLineTypes(Database db, IEnumerable<string> types)
        {
            using (var transaction = db.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(db.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;

                foreach (var name in types)
                {
                    if (!table.Has(name))
                    {
                        MessageBox.Show($"Не подгружен тип линии {name}");
                        return false;
                    }
                }
            }
            
            return true;
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Инициализировать плагин
        /// </summary>
        public void Initialize()
        {
            var doc = AApplication.DocumentManager.MdiActiveDocument;
            if (doc is null)
            {
                MessageBox.Show("При загрузке плагина произошла ошибка!", "Ошибка", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
            {
                var db = doc.Database;

                var typeNames = JObject
                    .Parse(File.ReadAllText(Path.Combine(Constants.SupportPath, "plugin.config.json")))
                    .Value<JArray>("LineTypes")
                    .Values<string>();

                foreach (var name in typeNames)
                {
                    db.LoadLineTypeFile(name, "acad.lin");
                }

                if (!VerifyLoadingLineTypes(db, typeNames)) return;

                doc.Editor.WriteMessage("Загрузка плагина прошла успешно!");
            }
        }
        /// <summary>
        /// Завершить работу плагина
        /// </summary>
        public void Terminate() 
        {
            File.Delete(Path.Combine(Path.GetTempPath(), DbConfigFilePath));
        }
#endregion

        #region Command Methods
        /// <summary>
        /// Отрисовать геометрию
        /// </summary>
        /// <exception cref="GotoException"></exception>
        [CommandMethod("MMP_DRAW")]
        public void DrawCommand()
        {
            OracleDbDispatcher connection = null;
            try
            {
#if DEBUG
                connection = new OracleDbDispatcher("Data Source=data-pc/GEO;Password=g1;User Id=g;Connection Timeout=360;");
                string gorizont = "K450E";
#else
                if (!OracleDbDispatcher.TryGetConnection(out connection)) return;

                string gorizont;

                using (var gorizontSelecter = new View.GorizontSelecterWindow(connection.Gorizonts))
                {
                    gorizontSelecter.ShowDialog();
                    if (!gorizontSelecter.InputResult)
                    {
                        return;
                    }
                    gorizont = gorizontSelecter.Gorizont;
                }
#endif
                new ObjectDispatcher(connection, gorizont).Draw();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n{ex.GetType()}\n{ex.StackTrace}");
            }
            finally
            {
                connection?.Dispose();
            }
        }
        /// <summary>
        /// Команда инспектирования отрисованных примитивов
        /// </summary>
        [CommandMethod("VRM_INSPECT_EXT_DB")]
        public void InspectExtDB()
        {
            if (!OracleDbDispatcher.TryGetConnection(out OracleDbDispatcher connection)) return;

            var document = AApplication.DocumentManager.MdiActiveDocument;
            var editor = document.Editor;
            var options = new PromptEntityOptions("\nВыберите объект: ");
            using (var transaction = document.TransactionManager.StartTransaction())
            {
                PromptEntityResult result = null;
                while ((result = editor.GetEntity(options)).Status == PromptStatus.OK)
                {
                    var buffer = transaction.GetObject(result.ObjectId, OpenMode.ForRead).XData;
                    if (buffer == null)
                    {
                        editor.WriteMessage("\nУ объекта отсутствуют параметры XData");
                        continue;
                    }

                    string xData;
                    const int ERROR_SYSTEM_ID = -1;

                    int systemId;
                    string[] row;
                    string linkField;

                    if ((linkField = buffer.GetXData(LINK_FIELD)) == string.Empty
                        || (systemId = (xData = buffer.GetXData(SYSTEM_ID)) == string.Empty ? ERROR_SYSTEM_ID : Convert.ToInt32(xData)) == ERROR_SYSTEM_ID
                        || (xData = buffer.GetXData(BASE_NAME)) == string.Empty
                        || (row = xData.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)).Length <= 1)
                    {
                        editor.WriteMessage("Атрибутивная таблица к объекту отсутствует!");
                        continue;
                    }
                        
                    string baseName = row[1];
                    string baseCapture = baseName;
                    var link = connection.GetExternalDbLink(baseName);

                    var fields = link.Split('\n');
                    var fieldNames = new Dictionary<string, string>();

                    if (fields.Length > 5) fieldNames = ParseFieldNames(fields);

                    if (fields.Length > 2) baseCapture = fields[1];

                    // FIXME: Неадо ли заполнять DataTable пустыми столбцами?
                    using (ExternalDBWindow window = new ExternalDBWindow(connection.GetDataTable(CreateCommand(baseName, linkField, systemId, fieldNames))))
                    {
                        window.Title = baseCapture;
                        window.ShowDialog();
                    }
                }
            }
        }
#endregion
        /// <summary>
        /// Создание команды выборки данных
        /// </summary>
        /// <param name="baseName">Имя линкованной таблицы</param>
        /// <param name="linkField">Столбец линковки</param>
        /// <param name="systemId">Уникальный номер примитива</param>
        /// <param name="fieldNames">Список столбцов таблицы</param>
        /// <returns>Команда для получения данных</returns>
        private string CreateCommand(string baseName, string linkField, int systemId, IDictionary<string, string> fieldNames)
        {
            var builder = new StringBuilder().Append("SELECT ");

            foreach (var item in fieldNames)
            {
                builder.Append(item.Key).Append(" as \"").Append(item.Value).Append("\"").Append(",");
            }

            builder
                .Remove(builder.Length - 1, 1)
                .Append(" FROM ")
                .Append(baseName)
                .Append(" WHERE ")
                .Append(linkField)
                .Append(" = ")
                .Append(systemId);

            return builder.ToString();
        }
        /// <summary>
        /// Получение списка столбцов таблицы
        /// </summary>
        /// <param name="fields">Исходные столбцы</param>
        /// <returns>Список столбцов</returns>
        private Dictionary<string, string> ParseFieldNames(IEnumerable<string> fields)
        {
            bool fieldsFlag = true;
            var result = new Dictionary<string, string>();

            foreach (var field in fields)
            {
                if (fieldsFlag)
                {
                    if (field == "FIELDS")
                    {
                        fieldsFlag = false;
                    }
                    continue;
                }
                else if (field == "ENDFIELDS")
                {
                    break;
                }
                else if (field.Contains("+"))
                {
                    continue;
                }
                var rows = field.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (rows.Length <= 1) continue;

                var builder = new StringBuilder();

                for (int j = 1; j < rows.Length; ++j)
                {
                    builder.Append(rows[j]).Append("_");
                }

                if (!result.ContainsKey(rows[0]))
                {
                    result.Add(rows[0], builder.ToString());
                }
            }

            return result;
        }
    }
}