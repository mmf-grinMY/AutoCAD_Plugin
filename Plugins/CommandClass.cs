#define DEBUG_1 // Проверка работоспособности плагина на K450E горизонте
#define LOAD_FONT // Подгрузка файла со шрифтами
// #define MULTI_THREAD // Отрисовка объектов как фоновая задача с показом прогресса
// #define LIMIT_1 // Ограничение количества рисуемых объектов равно 1

using System;
using System.IO;
using System.Windows;

using Oracle.ManagedDataAccess.Client;

using Newtonsoft.Json.Linq;

using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

namespace Plugins
{
    public class Commands : IExtensionApplication
    {
        #region Private Fields
        private readonly int scale = 1_000;
        private readonly string LEFT_BOUND = "LeftBound";
        private readonly string RIGHT_BOUND = "RightBound";
        private readonly string BOTTOM_BOUND = "BottomBound";
        private readonly string TOP_BOUND = "TopBound";
        #endregion

        #region Private Static Methods
        /// <summary>
        /// Сортировать объекты с учетом граничной рамки
        /// </summary>
        /// <param name="draw">Строковые параметры рисования</param>
        /// <param name="points">Граничные точки рамки</param>
        /// <returns>Параметры рисования</returns>
        /// <exception cref="GotoException">Вызывается, если объект не принадлежит рамке</exception>
        private DrawParams SortWithBoundingBox(Draw draw, Point3d[] points)
        {
            try
            {
                DrawParams drawParams = new DrawParams(draw);

                string value = drawParams.Param[LEFT_BOUND].Value<string>();
                if (value.Contains("1_=INF")) throw new GotoException(5);
                if (Convert.ToDouble(value.Replace("_", "")) * scale < points[0].X) throw new GotoException(5);
                if (Convert.ToDouble(drawParams.Param[BOTTOM_BOUND].Value<string>().Replace("_", "")) * scale < points[0].Y) throw new GotoException(5);
                if (Convert.ToDouble(drawParams.Param[RIGHT_BOUND].Value<string>().Replace("_", "")) * scale > points[1].X) throw new GotoException(5);
                if (Convert.ToDouble(drawParams.Param[TOP_BOUND].Value<string>().Replace("_", "")) * scale > points[1].Y) throw new GotoException(5);

                return drawParams;
            }
            catch
            {
                throw new GotoException(4);
            }
        }
        /// <summary>
        /// Сортировать параметры рисования
        /// </summary>
        /// <param name="draw">Строковые параметры рисования</param>
        /// <param name="points">Точки</param>
        /// <returns>Сконвертированные параметры рисования</returns>
        /// <exception cref="GotoException">Вызывается, если не удается сконвертировать параметры рисования</exception>
        private static DrawParams Sort(Draw draw, Point3d[] points)
        {
            try
            {
                return new DrawParams(draw);
            }
            catch
            {
                throw new GotoException(4);
            }
        }
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
                MessageBox.Show("При загрузке плагина произошла ошибка!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
            {
                var editor = doc.Editor;
                var db = doc.Database;

                // TODO: Добавить подгрузку типов линий

                // TODO: Добавить подгрузку шрифтов

                using (var transaction = db.TransactionManager.StartTransaction())
                {
                    var textStyleTable = transaction.GetObject(db.TextStyleTableId, OpenMode.ForWrite) as TextStyleTable ?? throw new ArgumentNullException($"Не удалось обратиться к объекту {nameof(TextStyleTable)}", nameof(TextStyleTable));

                    string path = Path.Combine(Directory.GetParent(Path.GetDirectoryName(db.Filename)).FullName, "Support", "pnt!.ttf").Replace("Local", "Roaming");

                    var record = new TextStyleTableRecord
                    {
                        Name = "pnt!.chr",
                        FileName = path
                    };

                    textStyleTable.Add(record);
                    transaction.AddNewlyCreatedDBObject(record, true);
                    transaction.Commit();
                }

                string helloMessage = "Загрузка плагина прошла успешно!";
                editor.WriteMessage(helloMessage);
            }
        }
        /// <summary>
        /// Завершить работу плагина
        /// </summary>
        public void Terminate() { }
#endregion

        #region Command Methods
// Тестовые команды и методы
#if DEBUG_COMMANDS
        [CommandMethod("MMP_LOADLINETYPE")]
        public void LoadLineType()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            // Укажите путь к вашему файлу шаблона линий
            string lineTypeFilePath = @"C:\Users\grinm\Documents\_Job_\_MapManager_\Programs\MapMan\UserData\ACAD\Linetype\MMLines.lin";

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LinetypeTable linetypeTable = trans.GetObject(db.LinetypeTableId, OpenMode.ForWrite) as LinetypeTable;

                if (linetypeTable != null)
                {
                    if (!linetypeTable.Has(lineTypeFilePath))
                    {

                        // Добавление нового типа линий в таблицу типов линий
                        LinetypeTableRecord linetypeRecord = new LinetypeTableRecord();
                        // linetypeRecord.Name = "Grantec";
                        // linetypeRecord.AsciiRepresentation = lineTypeFilePath;
                        linetypeTable.Add(linetypeRecord);
                        trans.AddNewlyCreatedDBObject(linetypeRecord, true);
                    }
                }

                trans.Commit();
            }

            doc.Editor.WriteMessage("Загрузка типа линии прошла успешно!");
        }
        /* Multi Thread Method Draw
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
        */
#endif

        /// <summary>
        /// Отрисовать геометрию
        /// </summary>
        /// <exception cref="GotoException"></exception>
        [CommandMethod("MMP_DRAW")]
        public void DrawCommand()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            OracleConnection connection = null;
            bool isBoundingBoxChecked = false;
            string connectionString = string.Empty;
            string gorizont = string.Empty;
            var loginWindow = new LoginWindow();

            var db = doc.Database;
#if !DEBUG_1
            void Login()
            {
                loginWindow.ShowDialog();
                if (loginWindow.InputResult)
                {
                    (connectionString, gorizont, isBoundingBoxChecked) = loginWindow.ConnectionString;
                }
            }

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
        connect:
            try
            {
#if DEBUG_1
                connection = new OracleConnection("Data Source=data-pc/GEO;Password=g1;User Id=g;Connection Timeout = 360;");
                connection.Open();

                var points = Array.Empty<Point3d>();
                Func<Draw, Point3d[], DrawParams> sort = Sort;
#if LIMIT_1
                int limit = 1;
#else
                int limit = int.MaxValue;
#endif
                gorizont = "K450E";
#else
                Login();
                connection = new OracleConnection(connectionString);
                connection.Open();
                loginWindow.Close();

                Point3d[] points = Array.Empty<Point3d>();
                Func<Draw, Point3d[], DrawParams> sort;

                if (isBoundingBoxChecked)
                {
                    points = GetBoundingBox();
                    sort = SortWithBoundingBox;
                }
                else
                {
                    sort = Sort;
                }

                int limit = 100;

                using (var reader = new OracleCommand($"SELECT count(*) FROM {gorizont}_trans_clone", connection).ExecuteReader())
                {
                    if (reader.Read())
                    {
                        limit = reader.GetInt32(0);
                    }
                }
#endif
                var args = new ObjectDispatcherCtorArgs(doc, gorizont, points, isBoundingBoxChecked, sort, connection, limit);
#if MULTI_THREAD
                var window = new WorkProgressWindow(args);
                window.ShowDialog();
#else
                using (var disp = new ObjectDispatcher(args))
                {
                    disp.Start(null);
                }
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
                // MessageBox.Show("Невозможно отрисовать объекты!");
                if (loginWindow.IsLoaded)
                    loginWindow.Close();
                connection?.Dispose();
            }
        }

#endregion
    }
}