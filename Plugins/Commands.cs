// TODO: Добавить в окно мониторинга за процессом отрисовки график заполнения очереди

// TODO: Добавить фильтр для выборки только определенных слоев

// TODO: Добавить панель со слоями как в MapManager

// TODO: Сделать нормальное масштабирование

// TODO: Сделать ограничение на отрисовку в одном чертеже только одного горизонта

// TODO: В случае неудачного подключения плагина формировать отчет о неудавшихся операциях в процессе загрузки

// TODO: Изменить модель чтения на ленивую
//       Запоминать текущую позицию и запускать читателя читать данные с текущей позиции с определенным количеством
//       только когда понадобятся новые данные

// #define POL // Команда рисования полилинии

#define FAST_DEBUG // Быстрая проверка на 305 горизонте

using Plugins.Logging;
using Plugins.View;

using System.Collections.Generic;
using System.Text;
using System;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

using static Plugins.Constants;

#if POL
using Autodesk.AutoCAD.Colors;
#endif


namespace Plugins
{
    public class Commands : IExtensionApplication
    {
        #region Private Fields

        static double width;
        static ILogger logger;

        #endregion

        #region Private Methods

        /// <summary>
        /// Взять граничные точки области
        /// </summary>
        /// <param name="doc">Текущий документ</param>
        /// <returns>Граничные точки, в случае успеха и UndefinedType в противном случае</returns>
        private static Point3d[] GetPoints(Editor editor)
        {
            var pointOptions = new PromptPointOptions("\n\tЛевый нижний угол: ");
            var left = editor.GetPoint(pointOptions);
            var cornerOptions = new PromptCornerOptions("\n\tПравый верхний угол: ", left.Value);
            var right = editor.GetCorner(cornerOptions);

            return right.Status == PromptStatus.OK && left.Status == PromptStatus.OK
                ? new Point3d[] { left.Value, right.Value }
                : null;
        }
        /// <summary>
        /// Создание команды выборки данных
        /// </summary>
        /// <param name="baseName">Имя линкованной таблицы</param>
        /// <param name="linkField">Столбец линковки</param>
        /// <param name="systemId">Уникальный номер примитива</param>
        /// <param name="fieldNames">Список столбцов таблицы</param>
        /// <returns>Команда для получения данных</returns>
        string CreateCommand(string baseName, string linkField, int systemId, IDictionary<string, string> fieldNames)
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
        Dictionary<string, string> ParseFieldNames(IEnumerable<string> fields)
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
        /// <summary>
        /// Вывести сообщение об ошибке
        /// </summary>
        /// <param name="message">Содержимое сообщения</param>
        /// <exception cref="ArgumentException"></exception>
        void ShowError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException(nameof(message));

            System.Windows.MessageBox.Show(message, "Ошибка",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Инициализировать плагин
        /// </summary>
        public void Initialize()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;

            if (doc is null)
            {
                System.Windows.MessageBox.Show("Невозможен доступ к активному документу!", "Ошибка",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            using (var view = doc.Editor.GetCurrentView()) { width = view.Width; }

            Editor editor = null;
            Database db;

            try
            {
                Constants.Initialize();

                db = doc.Database ?? throw new ArgumentNullException(nameof(db), "Не удалось получить ссылку на БД документа!");

                // FIXME: ??? Является ли ошибкой сбой получения командной строки текущего документа ???
                editor = doc.Editor ?? throw new ArgumentNullException(nameof(editor), 
                    "Не удалось получить ссылку на командную строку документа!");

                logger = Constants.Logger;

                if (logger is null) throw new ArgumentNullException("Не удалось получить логгер событий", nameof(logger));

                editor.WriteMessage("Загрузка плагина прошла успешно!\n");
            }
            catch (System.Exception e)
            {
                var errorMessage = "При загрузке плагина произошла ошибка!";

                Logger.LogError(e);

                if (editor is null)
                {
                    ShowError(errorMessage);
                }
                else
                {
                    editor.WriteMessage(errorMessage);
                }
            }
        }
        /// <summary>
        /// Завершить работу плагина
        /// </summary>
        public void Terminate()
        {
            dynamic app = Application.AcadApplication;

            try
            {
                System.IO.File.Delete(DbConfigPath);
                app.Preferences.Files.SupportPath = app.Preferences.Files.SupportPath.Replace(AssemblyPath, string.Empty);
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
            }
        }

        #endregion

        #region Command Methods

        public const string DRAW_COMMAND = "MMP_DRAW";
        /// <summary>
        /// Отрисовать геометрию
        /// </summary>
        [CommandMethod(DRAW_COMMAND)]
        public void DrawCommand()
        {
            Session session = null;

            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

                Point3d[] points = GetPoints(doc.Editor);

                session = new Session(logger);

                if (points != null && points.Length == 2) 
                {
                    var point = points[0];
                    var corner = points[1];

                    const double precession = 1000;
                    session.Left = (long)(Math.Min(point.X, corner.X) * precession);
                    session.Right = (long)(Math.Max(point.X, corner.X) * precession);
                    session.Bottom = (long)(Math.Min(point.Y, corner.Y) * precession);
                    session.Top = (long)(Math.Max(point.Y, corner.Y) * precession);
                }
                else
                {
                    session.Bottom = long.MinValue;
                    session.Left = long.MinValue;
                    session.Right = long.MaxValue;
                    session.Top = long.MaxValue;
                }

                // FIXME: !!! Если на горизонте 0 объектов, команда подвисает, а при закрытии окна AutoCAD выдает критическую ошибку !!!
                session.Run();
                logger.LogInformation("Закончена отрисовка геометрии!");
            }
            catch (TypeInitializationException)
            {
                logger.LogWarning("Не удалось подключиться к БД!");
                return;
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
            }
            finally
            {
                session?.Dispose();
            }
        }
        /// <summary>
        /// Команда инспектирования отрисованных примитивов
        /// </summary>
        [CommandMethod("VRM_INSPECT_EXT_DB")]
        public void InspectExtDB()
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument
                ?? throw new ArgumentNullException("document");

            var connection = new OracleDbDispatcher();

            var editor = doc.Editor;
            var options = new PromptEntityOptions("\nВыберите объект: ");
            using (var transaction = doc.TransactionManager.StartTransaction())
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

                    new ExternalDbWindow(connection.GetDataTable(CreateCommand(baseName, linkField, systemId, fieldNames)).DefaultView)
                    {
                        Title = baseCapture
                    }.ShowDialog();
                }
            }
        }

        #endregion
    }
    sealed class NotDrawingLineException : System.Exception { }
}