//#define MULTI_THREAD // Отрисовка объектов как фоновая задача с показом прогресса
//#define LIMIT_1 // Ограничение количества рисуемых объектов равно 1

#define DB_BOUNDING_BOX

// Из-за плохого сглаживания линий для приемлемого вида объектов при увеличении приходится вводить команду _REGEN
// Либо необходимо изменить переменные VIEWRES=20_000 и WHIPARC=1, что отразится на размере файла и производительности AutoCAD

using System;
using System.IO;
using System.Windows;

using Oracle.ManagedDataAccess.Client;

using AApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

using static Plugins.Constants;

namespace Plugins
{
    public partial class Commands : IExtensionApplication
    {
        #region Private Static Methods
#if OLD
        /// <summary>
        /// Взять граничные точки области
        /// </summary>
        /// <param name="doc">Текущий документ</param>
        /// <returns>Граничные точки, в случае успеха и UndefinedType в противном случае</returns>
        /// <exception cref="ArgumentException">Вызывается, если не удалось выбрать граничные точки рамки</exception>
        private static object GetPoints(Document doc)
        {
            var editor = doc.Editor;
            bool bound_fl = true;

            PromptPointOptions ppo = new PromptPointOptions("\n\tЛевый нижний угол: ");

            PromptPointResult pprLeft = editor.GetPoint(ppo);

            bound_fl = bound_fl && pprLeft.Status == PromptStatus.OK;

            PromptCornerOptions pco = new PromptCornerOptions("\n\tПравый верхний угол: ", pprLeft.Value);

            PromptPointResult pprRight = editor.GetCorner(pco);

            bound_fl = bound_fl && pprRight.Status == PromptStatus.OK;

            if (bound_fl)
            {
                return new Point3d[] { pprLeft.Value, pprRight.Value };
            }
            else
            {
                throw new ArgumentException(nameof(bound_fl));
            }
        }
#endif
        private static string GetXData(ResultBuffer rb, string RegAppName)
        {
            var proc_fl_1 = false;
            var result = string.Empty;
            foreach (var tv in rb)
            {
                if (proc_fl_1)
                {
                    result = tv.Value.ToString();
                    proc_fl_1 = false;
                }
                if ((tv.TypeCode == (int)DxfCode.ExtendedDataRegAppName) && (tv.Value.ToString() == RegAppName))
                {
                    proc_fl_1 = true;
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
                var editor = doc.Editor;
                var db = doc.Database;

                Constants.SetSupportPath(Path.Combine(Directory.GetParent(
                    Path.GetDirectoryName(db.Filename)).FullName, "Support").Replace("Local", "Roaming"));

                // TODO: Добавить подгрузку всех типов линий
                db.LoadLineTypeFile("Contur", Path.Combine(SupportPath, "linetype.lin"));

                editor.WriteMessage("Загрузка плагина прошла успешно!");
            }
        }
        /// <summary>
        /// Завершить работу плагина
        /// </summary>
        public void Terminate() { }
        #endregion

        #region Command Methods
        /// <summary>
        /// Отрисовать геометрию
        /// </summary>
        /// <exception cref="GotoException"></exception>
        [CommandMethod("MMP_DRAW")]
        public void DrawCommand()
        {
            var doc = AApplication.DocumentManager.MdiActiveDocument;
            OracleConnection connection = null;
            bool isBoundingBoxChecked = false;
            string connectionString = string.Empty;
            string gorizont = string.Empty;
            var loginWindow = new Plugins.View.LoginWindow();

            var db = doc.Database;
#if !DEBUG
            void Login()
            {
                loginWindow.ShowDialog();
                if (loginWindow.InputResult)
                {
                    (connectionString, gorizont, isBoundingBoxChecked) = loginWindow.ConnectionString;
                }
            }
#if !DB_BOUNDING_BOX
            Point3d[] GetBoundingBox()
            {
                do
                {
                    if (GetPoints(doc) is Point3d[] points)
                    {
                        return points;
                    }
                }
                while (true);
            }
#endif
#endif
        connect:
            try
            {
#if DEBUG
                connection = new OracleConnection("Data Source=data-pc/GEO;Password=g1;User Id=g;Connection Timeout=360;");
                connection.Open();

#if OLD
                var points = Array.Empty<Point3d>();
#endif
                gorizont = "K450E";
#else
                Login();
                connection = new OracleConnection(connectionString);
                connection.Open();
                loginWindow.Close();
#if OLD
                Point3d[] points = Array.Empty<Point3d>();

                if (isBoundingBoxChecked)
                {
                    points = GetBoundingBox();
                }
#endif
#endif
#if LIMIT_1
                int limit = 100;
#else
                int limit = 0;
                var query =
#if OLD
                $"SELECT count(*) FROM {gorizont}_trans_clone";
#else
                new SqlQuery().Count(gorizont + "_trans_clone").ToString();
#endif
                using (var reader = new OracleCommand(query, connection).ExecuteReader())
                {
                    if (reader.Read())
                    {
                        limit = reader.GetInt32(0);
                    }
                }
#endif
#if MULTI_THREAD
                var window = new WorkProgressWindow(args);
                window.ShowDialog();
#else
                new ObjectDispatcher(doc, gorizont, connection, limit).Start();
#endif
            }
            catch (OracleException ex)
            {
                if (ex.Number == 12154)
                {
                    if (MessageBox.Show("Неправильно указаны данные подключения к базе данных!\n\nЖелаете еще раз попробовать подключиться к базе данных?", "Ошибка подключения", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                    {
                        goto connect;
                    }
                }
                else if (ex.Number == 1017)
                {
                    MessageBox.Show("Неправильный логин или пароль!");
                    goto connect;
                }
                else
                {
                    MessageBox.Show(ex.Message);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n{ex.GetType()}\n{ex.StackTrace}");
            }
            finally
            {
                if (loginWindow.IsLoaded)
                    loginWindow.Close();
                connection?.Dispose();
            }
        }
        [CommandMethod("VRM_INSPECT_EXT_DB")]
        public void InspectExtDB()
        {
            VarOpenTrans vot = new VarOpenTrans();
            if (!vot.InitConnectionParams())
                return;
            if (!vot.TryGetPassword(out string username, out string password))
                return;
            if (!vot.TryConnectAndSave(new ConnectionParams(username, password)))
                return;

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
                    }
                    else
                    {
                        string xData;
                        const int ERROR_SYSTEM_ID = -1;

                        int systemId;
                        string[] row;
                        string linkField;

                        if ((linkField = GetXData(buffer, LINK_FIELD)) != string.Empty
                            && (systemId = (xData = GetXData(buffer, SYSTEM_ID)) == string.Empty ? ERROR_SYSTEM_ID : Convert.ToInt32(xData)) != ERROR_SYSTEM_ID
                            && (xData = GetXData(buffer, BASE_NAME)) != string.Empty && (row = xData.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)).Length > 1)
                        {
                            string baseName = row[1];
                            string baseCapture = baseName;
                            vot.ParseExternalDbLink(baseName, out string outData, vot.dbcon);
                            string[] f = outData.Split('\n');
                            if (f.Length > 2)
                                baseCapture = f[1];
                            vot.GetExternalDb(baseName, baseCapture, linkField, systemId, vot.dbcon);
                        }
                        else
                        {
                            editor.WriteMessage("Атрибутивная таблица к объекту отсутствует!");
                        }
                    }
                }
            }
            vot.dbcon.Close();
        }
#endregion

#if DEBUG_COMMANDS
        Func<DrawParameters> readAction = () =>
        {
            if (transactionReader.Read())
            {
                // draw read from db
                return draw;
            }
            else
            {
                Thread.CurrentThread.Abort();
                return null;
            }
        };

        Action<DrawParameters> writeAction = (draw) =>
        {
            // create layer if not exists
        };

        var pipeline = new Pipeline<DrawParameters>(readAction, writeAction, limitItemsCount: 1);
        var thread = new Thread(pipeline.Run);

        thread.Start();
        thread.Join();
#endif
    }
}