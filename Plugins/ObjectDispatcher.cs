#define ZOOM // Прибилижение к отрисовываемым объектам
// #define MULTI_THREAD // Отрисовывать объекты в качестве сторонней задачи
#define MY_BOUNDING_BOX // Отслеживание границ по координатам
#if !MY_BOUNDING_BOX
    #define DB_BOUNDING_BOX // Отслеживание границ по БД
#endif
// #define DEBUG_COUNTER // Вывод сообщения об отрисовке 1000 объектов

// #define MARK_SIGNS // Выбирать только знаки на слое Маркшейдерская сеть
// #define POLILINES // Отрисовка только полилиний

using Plugins.View;

using System;
using System.Windows;

using Oracle.ManagedDataAccess.Client;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using ALine = Autodesk.AutoCAD.DatabaseServices.Line;
using AApplication = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Plugins
{
    class Box
    {
        public long Left { get; set; } 
        public long Right { get; set; } 
        public long Top { get; set; } 
        public long Bottom { get; set; }
    }
    internal class ObjectDispatcher : IDisposable
    {
        #region Private Fields

        /// <summary>
        /// Текущий документ
        /// </summary>
        private readonly Document doc;
        /// <summary>
        /// Рисуемый горизонт
        /// </summary>
        private readonly string gorizont;
        // private readonly bool isBound;
        /// <summary>
        /// Граничные точки
        /// </summary>
        private readonly Point3d[] points;
        /// <summary>
        /// Метод сортировки объектов
        /// </summary>
        private readonly Func<Draw, Point3d[], DrawParams> sort;
        /// <summary>
        /// Подключение к БД
        /// </summary>
        private readonly OracleConnection connection;
        /// <summary>
        /// Предельное количество рисуемых объектов
        /// </summary>
        private readonly int limit;
        /// <summary>
        /// Количество нарисованных без ошибок объектов
        /// </summary>
        private int drawingCount = 0;
        /// <summary>
        /// Текущий слой
        /// </summary>
        private string currentLayer = string.Empty;
#if MY_BOUNDING_BOX
        readonly Box box = new Box() { Bottom = long.MaxValue, Left = long.MaxValue, Right = long.MinValue, Top = long.MinValue };
#endif

        #endregion

        #region Private Static Methods

        /// <summary>
        /// Проверить на наличие данных во всех столбцах строки
        /// </summary>
        /// <param name="dataReader">Читатель БД</param>
        /// <param name="length">Количество читаемых столбцов</param>
        /// <exception cref="GotoException">Вызывается, если один из столбцов хранит NULL</exception>
        /// <returns>true, если все столбцы содержат данные и false в противном случае</returns>
        private static bool IsDBNull(OracleDataReader dataReader, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (dataReader.IsDBNull(i))
                    throw new GotoException(i);
            }

            return false;
        }
        /// <summary>
        /// Прибилизить к рамке
        /// </summary>
        /// <param name="min">Минимальная точка</param>
        /// <param name="max">Максимальная точка</param>
        /// <param name="center">Центр рамки</param>
        /// <param name="factor">Масштаб приближения</param>
        private static void Zoom(Point3d min, Point3d max, Point3d center, double factor)
        {
            var doc = AApplication.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            int currentVPort = Convert.ToInt32(AApplication.GetSystemVariable("CVPORT"));
            var emptyPoint3d = new Point3d();

            if (min.Equals(emptyPoint3d) && max.Equals(emptyPoint3d))
            {
                if (db.TileMode || currentVPort != 1)
                {
                    min = db.Extmin;
                    max = db.Extmax;
                }
                else
                {
                    min = db.Pextmin;
                    max = db.Pextmax;
                }
            }

            using (var transaction = db.TransactionManager.StartTransaction())
            {
                using (ViewTableRecord view = doc.Editor.GetCurrentView())
                {
                    Extents3d extens3d;
                    Matrix3d matrixWCS2DCS =
                        (Matrix3d.Rotation(-view.ViewTwist, view.ViewDirection, view.Target) *
                        Matrix3d.Displacement(view.Target - Point3d.Origin) *
                        Matrix3d.PlaneToWorld(view.ViewDirection)).Inverse();
                    // If a center point is specified, define the min and max
                    // point of the extents
                    // for Center and Scale modes
                    if (center.DistanceTo(Point3d.Origin) != 0)
                    {
                        min = new Point3d(center.X - (view.Width / 2), center.Y - (view.Height / 2), 0);
                        max = new Point3d((view.Width / 2) + center.X, (view.Height / 2) + center.Y, 0);
                    }
                    // Create an extents object using a line
                    using (var line = new ALine(min, max))
                    {
                        (extens3d = new Extents3d(line.Bounds.Value.MinPoint, line.Bounds.Value.MaxPoint)).TransformBy(matrixWCS2DCS);
                    }
                    // Calculate the ratio between the width and height of the current view
                    double ratio = view.Width / view.Height;
                    double width;
                    double height;
                    Point2d newCenter;
                    // Check to see if a center point was provided (Center and Scale modes)
                    if (center.DistanceTo(Point3d.Origin) != 0)
                    {
                        width = view.Width;
                        height = view.Height;
                        if (factor == 0)
                        {
                            center = center.TransformBy(matrixWCS2DCS);
                        }
                        newCenter = new Point2d(center.X, center.Y);
                    }
                    else // Working in Window, Extents and Limits mode
                    {
                        // Calculate the new width and height of the current view
                        width = extens3d.MaxPoint.X - extens3d.MinPoint.X;
                        height = extens3d.MaxPoint.Y - extens3d.MinPoint.Y;
                        // Get the center of the view
                        newCenter = new Point2d((extens3d.MaxPoint.X + extens3d.MinPoint.X) * 0.5,
                                                (extens3d.MaxPoint.Y + extens3d.MinPoint.Y) * 0.5);
                    }
                    // Check to see if the new width fits in current window
                    if (width > (height * ratio))
                    {
                        height = width / ratio;
                    }
                    // Resize and scale the view
                    if (factor != 0)
                    {
                        view.Height = height * factor;
                        view.Width = width * factor;
                    }
                    view.CenterPoint = newCenter;
                    doc.Editor.SetCurrentView(view);
                }
                transaction.Commit();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Создать новый слой
        /// </summary>
        /// <param name="db">Внутренняя БД AutoCAD</param>
        /// <param name="layerName">Имя слоя</param>
        private void CreateLayer(Database db, string layerName)
        {
            if (currentLayer != layerName)
            {
                currentLayer = layerName;
                using (Transaction transaction = db.TransactionManager.StartTransaction())
                {
                    var layerTable = transaction.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                    if (layerTable.Has(layerName) == false)
                    {
                        LayerTableRecord layerTableRecord = new LayerTableRecord { Name = layerName };
                        layerTable.UpgradeOpen();
                        layerTable.Add(layerTableRecord);
                        transaction.AddNewlyCreatedDBObject(layerTableRecord, true);
                        db.Clayer = layerTable[layerName];
                    }

                    transaction.Commit();
                }
            }
        }
        /// <summary>
        /// Прочитать строку из БД
        /// </summary>
        /// <param name="reader">Читатель БД</param>
        /// <returns>Строковое представление параметров отрисовки</returns>
        private Draw Read(OracleDataReader reader)
        {
            if (!IsDBNull(reader, 5))
            {
                return new Draw(reader.GetString(1), reader.GetString(0), reader.GetString(2), $"{reader.GetString(3)} | {reader.GetString(4)}");
            }

            return new Draw();
        }
#endregion

        #region Ctors

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="args">Аргументы конструктора</param>
        public ObjectDispatcher(ObjectDispatcherCtorArgs args)
        {
            this.doc = args.Document;
            this.gorizont = args.Gorizont;
            this.points = args.Points;
            // this.isBound = args.IsBound;
            this.sort = args.Sort;
            this.connection = args.Connection;
            this.limit = args.Limit;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Освободить занятые ресурсы
        /// </summary>
        public void Dispose() { }
        /// <summary>
        /// Проделать полную итерацию отрисовки
        /// </summary>
        /// <param name="db">Внутренняя БД AutoCAD</param>
        /// <param name="reader">Читатель БД</param>
        public void PipelineIteration(Database db, OracleDataReader reader)
        {
            string layer = string.Empty;
            try
            {
#if DB_BOUNDING_BOX
                Draw InnerRead(OracleDataReader dataReader)
                {
                    if (IsDBNull(dataReader, 8))
                    {
                        throw new GotoException();
                    }
                    else
                    {
                        left = Math.Min(Convert.ToInt64(dataReader.GetString(4)), left);
                        right = Math.Max(Convert.ToInt64(dataReader.GetString(5)), right);
                        bottom = Math.Min(Convert.ToInt64(dataReader.GetString(6)), bottom);
                        top = Math.Max(Convert.ToInt64(dataReader.GetString(7)), top);
                        return new Draw(dataReader.GetString(1), dataReader.GetString(0), dataReader.GetString(2), dataReader.GetString(3));
                    }
                }

                var param = InnerRead(reader);                    
#else
                var param = Read(reader);
#endif
                var draw = sort(param, points);

                layer = draw.LayerName;
                CreateLayer(db, draw.LayerName);

                MMPFactory.Create(db, draw, box).Draw();
            }
            catch (GotoException)
            {
                return;
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.GetType() + "\n" + ex.Message + "\n" + ex.StackTrace + "\n" + ex.Source);
            }
        }
        /// <summary>
        /// Начать отрисовку объектов
        /// </summary>
        /// <param name="window">Окно отображения пргресса отрисовки</param>
        public void Start(WorkProgressWindow window)
        {
            var db = doc.Database;

            // SQL commands
            const string SELECT = "SELECT";
            const string FROM = "FROM";
            const string JOIN = "JOIN";
            const string ON = "ON";
            const string WHERE = "WHERE";
            const string AND = "AND";
            const string LIKE = "LIKE";
            const string NOT = "NOT";
            // Rows
            const string sublyaername = "sublayername";
            const string geowkt = "geowkt";
            const string drawjson = "drawjson";
            // Geometry types
            const string multilinestring = "MULTILINESTRING";

            string command =
                SELECT +
                " drawjson, geowkt, paramjson, layername, sublayername " +
#if DB_BOUNDING_BOX
                ", leftbound, rightbound, bottombound, topbound " +
#endif
                FROM +
                "(" +
                    SELECT + " b.layername, b.sublayername, a.geowkt, a.drawjson, a.paramjson, a.sublayerguid " +
#if DB_BOUNDIG_BOX
                ", a.leftbound, a.rightbound, a.topbound, a.bottombound " +
#endif
                    FROM + $" {gorizont}_trans_clone a " +
                    JOIN + $" {gorizont}_trans_open_sublayers b " +
                    ON + " a.sublayerguid = b.sublayerguid" +
#if MARK_SIGNS
                ")" +
                WHERE + " layername = 'Mapкшейдеpская сеть' " + AND + " sublayername = 'Знаки'";
#elif POLILINES
                ")"+ 
                $" {WHERE} {geowkt} {LIKE} '%{multilinestring}%' {AND} {drawjson} {NOT} {LIKE} '%\"BrushBkColor\": 0,%'"; 
#else
                ")";
#endif
            using (var reader = new OracleCommand(command, connection).ExecuteReader())
            {
                int counter = 0;
                string sublayer = string.Empty;

                while (reader.Read() && counter < limit)
                {
                    counter++;

#if DEBUG_COUNTER
                    if (counter % 1000 == 0)
                        MessageBox.Show(counter.ToString());
#endif

#if MULTI_THREAD
                    if (window.isCancelOperation)
                        return;

                    window.Dispatcher.Invoke(() =>
                    {
                        window.ReportProgress(counter);
                    });
#endif
                    PipelineIteration(db, reader);
                }
#if MULTI_THREAD
                window.Dispatcher.Invoke(() =>
                {
                    window.Close();
                });
#endif
                MessageBox.Show($"Закончена отрисовка геометрии!\n{drawingCount}");
#if ZOOM
                Zoom(new Point3d(box.Left, box.Bottom, 0), new Point3d(box.Right, box.Top, 0), new Point3d(0, 0, 0), 1.0);
#endif
            }
        }
#endregion
    }
}