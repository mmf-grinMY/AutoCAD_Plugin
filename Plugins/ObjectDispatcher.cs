#define ZOOM // Прибилижение к отрисовываемым объектам
// #define MULTI_THREAD // Отрисовывать объекты в качестве сторонней задачи
#define MY_BOUNDING_BOX // Отслеживание границ по координатам
#if !MY_BOUNDING_BOX
#define DB_BOUNDING_BOX // Отслеживание границ по БД
#endif

using Plugins.Entities;

using System;
using System.Windows;

using Oracle.ManagedDataAccess.Client;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using ALine = Autodesk.AutoCAD.DatabaseServices.Line;
using AApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Text.Json;

namespace Plugins
{
    internal class ObjectDispatcher
    {
        #region Private Fields

        private readonly EntitiesFactory factory;
        /// <summary>
        /// Текущий документ
        /// </summary>
        private readonly Document doc;
        /// <summary>
        /// Рисуемый горизонт
        /// </summary>
        private readonly string gorizont;
        /// <summary>
        /// Подключение к БД
        /// </summary>
        private readonly OracleConnection connection;
        /// <summary>
        /// Предельное количество рисуемых объектов
        /// </summary>
        private readonly int limit;
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
                    if (center.DistanceTo(Point3d.Origin) != 0)
                    {
                        min = new Point3d(center.X - (view.Width / 2), center.Y - (view.Height / 2), 0);
                        max = new Point3d((view.Width / 2) + center.X, (view.Height / 2) + center.Y, 0);
                    }
                    using (var line = new ALine(min, max))
                    {
                        (extens3d = new Extents3d(line.Bounds.Value.MinPoint, line.Bounds.Value.MaxPoint)).TransformBy(matrixWCS2DCS);
                    }
                    double ratio = view.Width / view.Height;
                    double width;
                    double height;
                    Point2d newCenter;
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
                    else
                    {
                        width = extens3d.MaxPoint.X - extens3d.MinPoint.X;
                        height = extens3d.MaxPoint.Y - extens3d.MinPoint.Y;
                        newCenter = new Point2d((extens3d.MaxPoint.X + extens3d.MinPoint.X) * 0.5,
                                                (extens3d.MaxPoint.Y + extens3d.MinPoint.Y) * 0.5);
                    }
                    if (width > (height * ratio))
                    {
                        height = width / ratio;
                    }
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

#if DB_BOUNDING_BOX
        /// <summary>
        /// Сортировать объекты с учетом граничной рамки
        /// </summary>
        /// <param name="draw">Строковые параметры рисования</param>
        /// <param name="points">Граничные точки рамки</param>
        /// <returns>Параметры рисования</returns>
        /// <exception cref="GotoException">Вызывается, если объект не принадлежит рамке</exception>
        private DrawParams Sort(Draw draw, Point3d[] points)
        {
            try
            {
                const string LEFT_BOUND = "LeftBound";
                const string RIGHT_BOUND = "RightBound";
                const string BOTTOM_BOUND = "BottomBound";
                const string TOP_BOUND = "TopBound";

                var drawParams = new DrawParams(draw);

                string value = drawParams.Param[LEFT_BOUND].Value<string>();
                if (value.Contains("1_=INF")) throw new GotoException(5);
                if (Convert.ToDouble(value.Replace("_", "")) * SCALE < points[0].X) throw new GotoException(5);
                if (Convert.ToDouble(drawParams.Param[BOTTOM_BOUND].Value<string>().Replace("_", "")) * SCALE < points[0].Y) throw new GotoException(5);
                if (Convert.ToDouble(drawParams.Param[RIGHT_BOUND].Value<string>().Replace("_", "")) * SCALE > points[1].X) throw new GotoException(5);
                if (Convert.ToDouble(drawParams.Param[TOP_BOUND].Value<string>().Replace("_", "")) * SCALE > points[1].Y) throw new GotoException(5);

                return drawParams;
            }
            catch
            {
                throw new GotoException(4);
            }
        }
#else
        /// <summary>
        /// Сортировать параметры рисования
        /// </summary>
        /// <param name="draw">Строковые параметры рисования</param>
        /// <param name="points">Точки</param>
        /// <returns>Сконвертированные параметры рисования</returns>
        /// <exception cref="GotoException">Вызывается, если не удается сконвертировать параметры рисования</exception>
        private DrawParams Sort(Draw draw)
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
#endif

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
                if (reader.IsDBNull(6) || reader.IsDBNull(7))
                    return new Draw(reader.GetString(1), reader.GetString(0), reader.GetString(2), $"{reader.GetString(3)} | {reader.GetString(4)}", reader.GetString(5), null);
                else
                    return new Draw(reader.GetString(1), reader.GetString(0), reader.GetString(2), $"{reader.GetString(3)} | {reader.GetString(4)}", reader.GetString(5), new LinkedDBFields(reader.GetString(6), reader.GetString(7)));
            }

            return new Draw();
        }
        #endregion

        #region Ctors

        public ObjectDispatcher(Document document, string gorizont, OracleConnection connection, int limit)
        {
            doc = document;
            this.gorizont = gorizont;
            this.connection = connection;
            this.limit = limit;
            factory = new EntitiesFactory();
        }

        #endregion

        #region Public Methods

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
                var draw = Sort(param);

                layer = draw.LayerName;
                CreateLayer(db, draw.LayerName);
                using (var entity = factory.Create(db, draw, box))
                {
                    entity?.Draw();
                }
            }
            catch (GotoException)
            {
                return;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                MessageBox.Show(ex.GetType() + "\n" + ex.Message + "\n" + ex.StackTrace + "\n" + ex.Source);
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
        public void Draw()
        {
            var db = doc.Database;

            // SQL commands
            const string SELECT = "SELECT";
            const string FROM = "FROM";
            const string JOIN = "JOIN";
            const string ON = "ON";
            // Rows
            const string layername = "layername";
            const string sublayername = "sublayername";
            const string geowkt = "geowkt";
            const string drawjson = "drawjson";
            const string paramjson = "paramjson";
            const string sublayerguid = "sublayerguid";
            // Linked table
            const string systemid = "systemid";
            const string basename = "basename";
            const string childfields = "childfields";

            // TODO: Переписать SQL-запрос специальным классом
            string command =
                SELECT +
                $" {drawjson}, {geowkt}, {paramjson}, {layername}, {sublayername}, " +
                $"{systemid}, {basename}, {childfields} " +
#if DB_BOUNDING_BOX
                ", leftbound, rightbound, bottombound, topbound " +
#endif
                FROM +
                "(" +
                    SELECT +
                    $" b.{layername}, b.{sublayername}, a.{geowkt}, a.{drawjson}, a.{paramjson}, a.{sublayerguid}, " +
                    $"a.{systemid}, b.{childfields}, b.{basename} " +
#if DB_BOUNDIG_BOX
                ", a.leftbound, a.rightbound, a.topbound, a.bottombound " +
#endif
                    FROM + $" {gorizont}_trans_clone a " +
                    JOIN + $" {gorizont}_trans_open_sublayers b " +
                    ON + $" a.{sublayerguid} = b.{sublayerguid}" +
                ")";
            using (var reader = new OracleCommand(command, connection).ExecuteReader())
            {
                int counter = 0;
                while (reader.Read() && counter < limit)
                {
                    counter++;
                    PipelineIteration(db, reader);
                }
            }
            MessageBox.Show($"Закончена отрисовка геометрии!");
            Zoom(new Point3d(box.Left, box.Bottom, 0), new Point3d(box.Right, box.Top, 0), new Point3d(0, 0, 0), 1.0);
        }
        #endregion
    }
}