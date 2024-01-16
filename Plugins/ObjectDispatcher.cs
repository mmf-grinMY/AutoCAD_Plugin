#define ALL_LAYERS // отрисовка всех слоев
// #define MARK_LAYER // отрисовка слоя 'Маркшейдерские поля'
#define TEXT // отрисовка подслоя 'Текст'
#define LINE // отрисовка подслоя 'Линия'
// #define LAYERS // деление на слои
#define ZOOM // прибилижение к отрисовываемым объектам
// #define MULTI_THREAD
#define MY_BOUNDING_BOX
#define BOUNDING_BOX // Учет bounding box, определенной в таблице
// #define GET_LAYER // Функция для отдельного запроса к БД для изъятия параметров слоя
// #define COUNTER_SHOW
// #define LAYER_DEBUG // просмотр неправильности отрисовки некоторых слоев

using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Windows;
using Line = Autodesk.AutoCAD.DatabaseServices.Line;
using SColor = System.Drawing.Color;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using AColor = Autodesk.AutoCAD.Colors.Color;
using Oracle.ManagedDataAccess.Client;
using Autodesk.AutoCAD.Geometry;
using Aspose.Gis.Geometries;
using Newtonsoft.Json.Linq;
using Plugins.View;
using Autodesk.AutoCAD.ApplicationServices;

namespace Plugins
{
    internal class ObjectDispatcher : IDisposable
    {
        #region Private Fields

        /// <summary>
        /// Масштаб всех объектов по отношению к записям БД
        /// </summary>
        private readonly int scale = 1_000;
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
        /// <summary>
        /// Масштаб текста
        /// </summary>
        private readonly int textScale = 750;
#if MY_BOUNDING_BOX
        /// <summary>
        /// Крайняя левая точка рамки
        /// </summary>
        private long left = long.MaxValue;
        /// <summary>
        /// Крайняя правая точка рамки
        /// </summary>
        private long right = long.MinValue;
        /// <summary>
        /// Крайняя верхняя точка рамки
        /// </summary>
        private long top = long.MinValue;
        /// <summary>
        /// Крайняя нижняя точка рамки
        /// </summary>
        private long bottom = long.MaxValue;
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
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            int currentVPort = Convert.ToInt32(Application.GetSystemVariable("CVPORT"));
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
                    using (var line = new Line(min, max))
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
        /// Отрисовать объект
        /// </summary>
        /// <param name="db">Внутренняя БД AutoCAD</param>
        /// <param name="draw">Параметры рисования</param>
        /// <exception cref="NotImplementedException">Вызывается, если не предусмотрена логика отрисовки объекта</exception>
        private void DrawPrimitives(Database db, DrawParams draw)
        {
            switch (draw.DrawSettings["DrawType"].ToString())
            {
                case "Polyline":
                    {
                        switch (draw.Geometry)
                        {
                            case MultiLineString multiLine:
                                {
                                    foreach (var line in multiLine)
                                    {
                                        DrawPolyline(db, (LineString)line, draw.DrawSettings, draw.LayerName);
                                    }
                                    drawingCount++;
                                }
                                break;
                            case Polygon polygon:
                                {
                                    DrawPolygon(db, polygon, draw.DrawSettings, draw.LayerName);
                                    drawingCount++;
                                }
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    break;
                case "TMMTTFSignDrawParams":
                    break;
                case "BasicSignDrawParams":
                    break;
                case "LabelDrawParams":
                    {
                        if (draw.Geometry is Aspose.Gis.Geometries.Point point)
                        {
                            drawingCount++;
                            DrawText(db, new Point3d(point.X * scale, point.Y * scale, 0), draw.DrawSettings, draw.LayerName);
                        }
                    }
                    break;
                default: throw new NotImplementedException();
            }
        }
        /// <summary>
        /// Нарисовать абстрактный объект
        /// </summary>
        /// <param name="db">Внутренняя БД AutoCAD</param>
        /// <param name="action">Логика отрисовки</param>
        private void DrawEntity(Database db, Action<Transaction, BlockTableRecord> action)
        {
            using (var transaction = db.TransactionManager.StartTransaction())
            {
                using (var blockTable = transaction.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable)
                {
                    using (var record = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord)
                    {
                        action(transaction, record);
                        transaction.Commit();
                    }
                }
            }
        }
        /// <summary>
        /// Нарисовать полигон
        /// </summary>
        /// <param name="db">Внутренняя БД AutoCAD</param>
        /// <param name="polygon">Полигон для отрисовки</param>
        /// <param name="settings">Параметры легендаризации полигона</param>
        /// <param name="layer">Имя слоя</param>
        private void DrawPolygon(Database db, Polygon polygon, JObject settings, string layer)
        {
            void action(Transaction transaction, BlockTableRecord record)
            {
                using (var polyline = new Polyline())
                {
                    try
                    {
                        var line = polygon.ReplacePolygonsByLines() as LineString ?? throw new ArgumentNullException(nameof(polygon));
                        ActionDrawPolyline(polyline, transaction, record, line, settings, layer);
                        var region = Autodesk.AutoCAD.DatabaseServices.Region.CreateFromCurves(new DBObjectCollection { polyline })[0]
                            as Autodesk.AutoCAD.DatabaseServices.Region ?? throw new ArgumentNullException(nameof(polyline));
                        region.Color = AColor.FromColor(SColor.FromArgb(Convert.ToInt32(settings["BrushBkColor"].ToString())));
                        record.AppendEntity(region);
                        transaction.AddNewlyCreatedDBObject(region, true);
                        var objIdCollection = new ObjectIdCollection { region.ObjectId };

                        using (var hatch = new Hatch())
                        {
                            record.AppendEntity(hatch);
                            transaction.AddNewlyCreatedDBObject(hatch, true);

                            string pattern = settings["BitmapName"].Value<string>() + settings["BitmapIndex"].Value<string>();

                            // FIXME: Добавить оригинальную щаливку с этим номером
                            if (pattern != "DRO3247")
                                hatch.SetHatchPattern(HatchPatternType.CustomDefined, pattern);
                            else
                                hatch.SetHatchPattern(HatchPatternType.UserDefined, "SOLID");
                            hatch.Layer = layer;
                            hatch.Associative = true;
                            hatch.AppendLoop(HatchLoopTypes.Outermost, objIdCollection);
                            hatch.EvaluateHatch(true);
                        }
                    }
                    catch (ArgumentNullException)
                    {
                        // TODO: Предусмотреть перерисовку объекта своими методами
                    }
                    catch (ArgumentException)
                    {
                        // TODO: Предусмотреть перерисовку объекта своими методами
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message != "eInvalidInput")
                        {
                            throw ex;
                        }
                    }
                }
            }

            DrawEntity(db, action);
        }
        /// <summary>
        /// Проверить двумерную точку на принадлежность рамке
        /// </summary>
        /// <param name="point">Проверяемая точка</param>
        private void CheckBoundingBox(Point2d point)
        {
            left = Convert.ToInt64(Math.Min(point.X, left));
            right = Convert.ToInt64(Math.Max(point.X, right));
            bottom = Convert.ToInt64(Math.Min(point.Y, bottom));
            top = Convert.ToInt64(Math.Max(point.Y, top));
        }
        /// <summary>
        /// Проверить трехмерную точку на принадлежность рамке
        /// </summary>
        /// <param name="point">Проверяемая точка</param>
        private void CheckBoundingBox(Point3d point)
        {
            left = Convert.ToInt64(Math.Min(point.X, left));
            right = Convert.ToInt64(Math.Max(point.X, right));
            bottom = Convert.ToInt64(Math.Min(point.Y, bottom));
            top = Convert.ToInt64(Math.Max(point.Y, top));
        }
        /// <summary>
        /// Отрисовать основу полилинии
        /// </summary>
        /// <param name="polyline">Отрисовываемая полилиния</param>
        /// <param name="transaction">Текущая транзакция в БД</param>
        /// <param name="record">Таблица записи</param>
        /// <param name="line">Текущая линия</param>
        /// <param name="settings">Параметры легендаризации</param>
        /// <param name="layer">Имя слоя</param>
        private void ActionDrawPolyline(Polyline polyline, Transaction transaction, BlockTableRecord record, LineString line, JObject settings, string layer)
        {
            for (int i = 0; i < line.Count; i++)
            {
                var point = new Point2d(line[i].X * scale, line[i].Y * scale);
#if MY_BOUNDING_BOX
                CheckBoundingBox(point);
#endif
                polyline.AddVertexAt(i, point, 0, 0, 0);
            }

            // TODO: Добавить тип линий
            //polyline.Linetype = "Grantec";
            polyline.Color = AColor.FromColor(SColor.FromArgb(Convert.ToInt32(settings["BrushColor"].ToString())));
            polyline.Thickness = Convert.ToInt32(settings["Width"].ToString());
            polyline.Layer = layer;

            record.AppendEntity(polyline);
            transaction.AddNewlyCreatedDBObject(polyline, true);
        }
        /// <summary>
        /// Отрисовать полилиню
        /// </summary>
        /// <param name="db">Внутренняя БД AutoCAD</param>
        /// <param name="line">Исходная линия</param>
        /// <param name="settings">Параметры легендаризации</param>
        /// <param name="layer">Имя слоя</param>
        private void DrawPolyline(Database db, LineString line, JObject settings, string layer)
        {
            void action(Transaction transaction, BlockTableRecord record)
            {
                using (var polyline = new Polyline())
                {
                    ActionDrawPolyline(polyline, transaction, record, line, settings, layer);
                }
            }

            DrawEntity(db, action);
        }
        /// <summary>
        /// Отрисовать однострочный текст
        /// </summary>
        /// <param name="db">Внутренняя БД AutoCAD</param>
        /// <param name="point">Исходная позиция текста</param>
        /// <param name="settings">Параметры легендаризации</param>
        /// <param name="layer">Имя слоя</param>
        private void DrawText(Database db, Point3d point, JObject settings, string layer)
        {
            void action(Transaction transaction, BlockTableRecord record)
            {
                var text = new DBText();
                text.SetDatabaseDefaults();
                text.Position = point;
                int fontSize = Convert.ToInt32(settings["FontSize"].Value<string>());
                if (fontSize > 0)
                    text.Height = fontSize * textScale;
                text.TextString = settings["Text"].Value<string>();
                text.Layer = layer;

#if MY_BOUNDING_BOX
                CheckBoundingBox(point);
#endif

                record.AppendEntity(text);
                transaction.AddNewlyCreatedDBObject(text, true);
            }

            DrawEntity(db, action);
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
        public void Dispose()
        {
#if LOG
            writer.Close();
#endif
        }
        /// <summary>
        /// Приблизить к граничной рамке
        /// </summary>
        public void Zoom() => Zoom(new Point3d(left, bottom, 0), new Point3d(right, top, 0), new Point3d(0, 0, 0), 1.0);
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
#if ZOOM && !MY_BOUNDING_BOX
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

                DrawPrimitives(db, draw);
            }
            catch (Exception) 
            {
                MessageBox.Show($"Не удалось создать слой {layer}!");
            }
        }
        /// <summary>
        /// Начать отрисовку объектов
        /// </summary>
        /// <param name="window">Окно отображения пргресса отрисовки</param>
        public void Start(WorkProgressWindow window)
        {
            var db = doc.Database;

            const string columns = "drawjson, geowkt, paramjson";

#if ZOOM && !MY_BOUNDING_BOX
            long 
                left = long.MaxValue, 
                right = long.MinValue, 
                bottom = long.MaxValue, 
                top = long.MinValue;
#endif
            // TODO: Переделать директивы препроцессора на if (isBoungingBox)
#if MARK_LAYER && LINE
            string command = $"SELECT drawjson, geowkt, paramjson, sublayerguid FROM {gorizont}_trans_clone";
#else
            string command =
                "SELECT " + columns +
#if ZOOM
                ", layername, sublayername, leftbound, rightbound, bottombound, topbound " +
#endif
                "FROM" +
                "(" +
                "     SELECT b.layername, b.sublayername, a.geowkt, a.drawjson, a.paramjson, a.sublayerguid" +
#if ZOOM
                ", a.leftbound, a.rightbound, a.topbound, a.bottombound " +
#endif
                $"     FROM {gorizont}_trans_clone a" +
                $"     JOIN {gorizont}_trans_open_sublayers b" +
                "     ON a.sublayerguid = b.sublayerguid" +
                ")";
#endif
            using (var reader = new OracleCommand(command, connection).ExecuteReader())
            {
                int counter = 0;
                string sublayer = string.Empty;

                while (reader.Read() && counter < limit)
                {
                    counter++;
#if MULTI_THREAD
                    if (window.isCancelOperation)
                        return;

                    window.Dispatcher.Invoke(() =>
                    {
                        window.ReportProgress(counter);
                    });
#endif
                    try
                    {
                        PipelineIteration(db, reader);
                    }
                    catch (GotoException)
                    {
                        continue;
                    }
                }
#if MULTI_THREAD
                window.Dispatcher.Invoke(() =>
                {
                    window.Close();
                });
#endif
                MessageBox.Show($"Закончена отрисовка геометрии!\n{drawingCount}");
#if ZOOM && MY_BOUNDING_BOX
                Zoom(new Point3d(left, bottom, 0), new Point3d(right, top, 0), new Point3d(0, 0, 0), 1.0);
#else
                if (isBound)
                    Zoom(points[0], points[1], new Point3d(0, 0, 0), 1.0);
#endif
            }
        }
        #endregion
    }
}