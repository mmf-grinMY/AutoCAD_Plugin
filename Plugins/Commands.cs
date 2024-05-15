using Plugins.Logging;

using System.Collections.Generic;
using System.Linq;
using System;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

using Oracle.ManagedDataAccess.Client;

using static Plugins.Constants;
using Plugins.View;
using System.Security.Cryptography;

namespace Plugins
{
    public class Commands : IExtensionApplication
    {
        #region Private Methods

        /// <summary>
        /// Взять граничные точки области
        /// </summary>
        /// <param name="doc">Текущий документ</param>
        /// <returns>Граничные точки, в случае успеха и UndefinedType в противном случае</returns>
        IEnumerable<Point3d> GetPoints(Editor editor)
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

            Editor editor = null;
            Database db;
            ILogger logger = null;

            try
            {
                Constants.Initialize();

                logger = new FileLogger(nameof(Initialize));
                db = doc.Database ?? throw new ArgumentNullException(nameof(db), "Не удалось получить ссылку на БД документа!");
                editor = doc.Editor;

                editor.WriteMessage("Загрузка плагина прошла успешно!\n");
            }
            catch (System.Exception e)
            {
                var errorMessage = "При загрузке плагина произошла ошибка!" + Environment.NewLine + e.Message;
                logger.LogError(e);

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
            try
            {
                System.IO.File.Delete(DbConfigPath);
                UnloadFriendFolders();
            }
            catch (System.Exception e)
            {
                var logger = new FileLogger(nameof(Terminate));
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
            var logger = new FileLogger("main.log");

            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

                try
                {
                    session = new Session(logger)
                    {
                        Bottom = long.MinValue,
                        Left = long.MinValue,
                        Right = long.MaxValue,
                        Top = long.MaxValue
                    };
                }
                catch (InvalidOperationException) 
                {
                    logger.LogInformation("Выполнение команды \"" + DRAW_COMMAND + "\" была остановлена пользователем!");
                    return; 
                }

                if (session.IsBoundingBoxChecked)
                {
                    var points = GetPoints(doc.Editor);

                    if (points != null && points.Count() == 2)
                    {
                        var point = points.ElementAt(0);
                        var corner = points.ElementAt(1);

                        const double precession = 1000;
                        session.Left = (long)(Math.Min(point.X, corner.X) * precession);
                        session.Right = (long)(Math.Max(point.X, corner.X) * precession);
                        session.Bottom = (long)(Math.Min(point.Y, corner.Y) * precession);
                        session.Top = (long)(Math.Max(point.Y, corner.Y) * precession);
                    }
                }

                session.Run();
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
        /// Команда инспектирования атрибутивной таблицы
        /// </summary>
        [CommandMethod("VRM_INSPECT_EXT_DB")]
        public void InspectExtDB()
        {
            OracleDbDispatcher connection;

            try
            {
                connection = new OracleDbDispatcher();
            }
            catch (InvalidOperationException) { return; }

            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
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
                        || (systemId = (xData = buffer.GetXData(SYSTEM_ID)) == string.Empty
                            ? ERROR_SYSTEM_ID
                            : Convert.ToInt32(xData)) == ERROR_SYSTEM_ID
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

                    if (fields.Length > 5) fieldNames = DbHelper.ParseFieldNames(fields);

                    if (fields.Length > 2) baseCapture = fields[1];

                    var form = new ExternalDB();
                    form.dataGridView1.DataSource = connection.GetDataTable(DbHelper.CreateCommand(baseName, linkField, systemId, fieldNames));
                    form.Text = baseCapture;
                    form.ShowDialog();
                }
            }
        }

        #endregion
    }
    sealed class NotDrawingLineException : System.Exception { }
}