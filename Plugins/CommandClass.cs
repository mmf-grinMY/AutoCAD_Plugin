// TODO: Написать свой конвертер из WKT Polygon
// Пункты замера мощностей ??? Нет объектов, принадлежащих слою

// TODO: Убрать все using у объектов, полученных через транзакцию

// Убрать все MesssageBox, которые не связаны с подключением к БД

// Нужны данные для отрисовки, может попробовать реверснуть mpr?

using Plugins.View;

using System.Collections.Generic;
using System.Windows;
using System.Text;
using System.Linq;
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
        /// <param name="count">Количество подгруженных стилей</param>
        /// <returns>true, если все стили корректно загружены, false в противном случае</returns>
        private bool VerifyLoadingLineTypes(Database db, int count)
        {
            using (var transaction = db.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(db.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;

                for (int i = 0; i < count; ++i)
                {
                    var name = LineTypeLoader.STYLE_NAME + (i + 1);

                    if (!table.Has(name))
                    {
                        MessageBox.Show($"Не подгружен тип линии {name}");
                        return false;
                    }
                }
            }
            
            return true;
        }
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
                try
                {
                    var db = doc.Database;

                    Constants.OldView = doc.Editor.GetCurrentView();

                    doc.ViewChanged += (s, e) =>
                    {
                        doc.Editor.WriteMessage(e.ToString());

                        // TODO: Найти базовый scale
                        // Отталкиваясь от него перерисовать все объекты, находящиеся в области экрана
#if false
                        var view = doc.Editor.GetCurrentView();
                        doc.Editor.WriteMessage($"{view.Height}x{view.Width}\n");
                        var min = view.Bounds.Value.MinPoint;
                        var max = view.Bounds.Value.MaxPoint;
                        using (var transaction = db.TransactionManager.StartTransaction())
                        {
                            var table = transaction.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                            var record = transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                            // TODO: Сделать итерацию только по слоям, которые содержат масштабируемые объекты
                            // Типизация штрихованных линий
                            // Масштаб текста
                            // Масштаб штриховки
                            foreach (var id in record)
                            {
                                var entity = transaction.GetObject(id, OpenMode.ForRead) as Entity;

                                var bound = entity.Bounds.Value;
                                var pMin = bound.MinPoint;
                                var pMax = bound.MaxPoint;
                                if ((pMax.X < min.X) || (pMin.X > max.X) || (pMax.Y < min.Y) || (pMin.Y > max.Y))
                                {

                                }
                            }

                            transaction.Commit();
                        }
#endif
                    };

                    int count = new LineTypeLoader().Load();
                    for (int i = 0; i < count; ++i)
                    {
                        db.LoadLineTypeFile(LineTypeLoader.STYLE_NAME + (i + 1).ToString(), LineTypeLoader.STYLE_FILE);
                    }

                    if (!VerifyLoadingLineTypes(db, count))
                    {
                        throw new LineTypeNotFoundException();
                    }

                    doc.Editor.WriteMessage("Загрузка плагина прошла успешно!");
                }
                catch (LineTypeNotFoundException)
                {
                    MessageBox.Show("Не удалось найти все стили линии!");
                    return;
                }
            }
        }
        /// <summary>
        /// Завершить работу плагина
        /// </summary>
        public void Terminate() 
        {
            File.Delete(Path.Combine(Path.GetTempPath(), DbConfigFilePath));
            File.Delete(Path.Combine(SupportPath, LineTypeLoader.STYLE_FILE));
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
            // TODO: Добавить поддержку пользовательского BoundingBox
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
            catch (CtorException)
            {
                return;
            }
            catch (System.Exception ex)
            {
#if !RELEASE
                MessageBox.Show($"Error: {ex.Message}\n{ex.GetType()}\n{ex.StackTrace}");
#endif
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

                    using (ExternalDbWindow window = new ExternalDbWindow(connection.GetDataTable(CreateCommand(baseName, linkField, systemId, fieldNames)).DefaultView))
                    {
                        window.Title = baseCapture;
                        window.ShowDialog();
                    }
                }
            }
        }
        #endregion
    }
    sealed class LineTypeLoader
    {
        public static readonly string STYLE_FILE = ".tmp.lin";
        public static readonly string STYLE_NAME = "MM_LineType";
        public int Load()
        {
            var content = File.ReadAllLines(Path.Combine(Constants.SupportPath, "acad.lin"));

            var styles = JObject
                .Parse(File.ReadAllText(Path.Combine(Constants.SupportPath, "plugin.config.json")))
                .Value<JArray>("LineTypes")
                .Values<string>();

            int counter = 0;

            using (var stream = new StreamWriter(Path.Combine(SupportPath, STYLE_FILE), false))
            {
                for (int i = 0; i < content.Length; ++i)
                {
                    var line = content[i];
                    if (line.StartsWith(";;"))
                    {
                        continue;
                    }
                    else if (line.StartsWith("*"))
                    {
                        foreach (var style in styles)
                        {
                            if (line.StartsWith("*" + style))
                            {
                                stream.WriteLine("*" + STYLE_NAME + (++counter).ToString());
                                var descriptionType = content[i + 1];
                                var numbers = descriptionType.Substring(2, descriptionType.Length - 2).Split(',');
                                var builder = new StringBuilder().Append('A');
                                foreach(var number in numbers)
                                {
                                    builder.Append(',').Append(Convert.ToDouble(number.StartsWith(".") ? "0" + number : number) * SCALE);
                                }
                                stream.WriteLine(builder.ToString());
                                ++i;
                            }
                        }
                    }
                }
            }

            if (counter == styles.Count())
                return counter;
            else
                throw new LineTypeNotFoundException();
        }
    }
    sealed class LineTypeNotFoundException : System.Exception { }
}