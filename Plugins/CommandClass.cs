#region Usings

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Xml;
using Newtonsoft.Json;
using Line = Autodesk.AutoCAD.DatabaseServices.Line;
using SColor = System.Drawing.Color;
using Exception = System.Exception;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using AColor = Autodesk.AutoCAD.Colors.Color;
using Oracle.ManagedDataAccess.Client;
using System.IO;
using System.Threading;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using Aspose.Gis;
using System.Windows.Media;
using Aspose.Gis.Geometries;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Windows.Controls;

#endregion

namespace Plugins
{
    public class Commands : IExtensionApplication
    {
        public void Initialize()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc is null)
            {
                MessageBox.Show("При загрузке плагина произогла ошибка!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
            {
                var editor = doc.Editor;
                string helloMessage = "Загрузка плагина прошла успешно!";
                editor.WriteMessage(helloMessage);
            }
        }
        /* Multi Thread Method Draw
        [CommandMethod("Draw")]
        public void Draw()
        {
            // 198
            #region Local variables init

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc is null)
            {
                MessageBox.Show("Во время запуска команды произошла ошибка!", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string root = Directory.GetCurrentDirectory();
            Database db = doc.Database;
            XmlConnection conn = null;
            OracleConnection connection = null;
            OracleDataReader transactionReader = null;
            // Взятие граничных точек
            bool GET_POINTS = true;
            Point3d[] points = new Point3d[2];
            string sublayer = string.Empty;
            LayerTable layerTable;
            HashSet<string> sublayers = new HashSet<string>();
            Func<DrawParameters> readAction = null;

            #endregion

            #region Bounds
            const string bottomBound = "BottomBound";
            const string topBound = "TopBound";
            const string leftBound = "LeftBound";
            const string rightBound = "RightBound";
            #endregion

            try
            {
                #region Window initialize

                WindowVars vars;
#if RELEASE
                var window = new LoginWindow();
                bool? resultDialog = window.ShowDialog();

                if (resultDialog.HasValue)
                {
                    vars = window.Vars;
                }
                else
                {
                    MessageBox.Show("Для отрисовки объектов требуются данные!");
                    return;
                }
#else
                // GEO
                vars = new DBWindowVars("g", "g", "data-pc/GEO", "NORMAL", "k670f_trans_clone", "k670f_trans_open_sublayers");
                // XEPDB1
                // vars = new DBWindowVars("sys", "SYSTEM", "localhost/XEPDB1", "SYSDBA", "k670f_trans_open", "");
                
#endif
#endregion

                switch (vars)
                {
                    case DBWindowVars windowVars:
                        string conStringUser = $"Data Source={windowVars.Host};Password={windowVars.Password};User Id={windowVars.Username};";
                        conStringUser += windowVars.Privilege == "NORMAL" ? string.Empty : $"DBA Privilege = {windowVars.Privilege};";
                        connection = new OracleConnection(conStringUser);
                        
                        if (connection is null)
                        {
                            MessageBox.Show("Не удалось подключиться к базе данных!");
                        }

                        connection.Open();
                        transactionReader = new OracleCommand("SELECT drawjson, geowkt, sublayerguid, paramjson FROM " + windowVars.TransactionTableName, connection).ExecuteReader();

                        readAction = () =>
                        {
                            if (transactionReader.Read())
                            {
                                var draw = new DrawParameters
                                {
                                    DrawSettings = transactionReader.IsDBNull(0) ? DrawSettings.Empty : JsonConvert.DeserializeObject<DrawSettings>(transactionReader.GetString(0)),
                                    WKT = transactionReader.IsDBNull(1) ? string.Empty : transactionReader.GetString(1),
                                    SubleyerGUID = transactionReader.IsDBNull(2) ? string.Empty : transactionReader.GetString(2),
                                    Param = transactionReader.IsDBNull(3) ? string.Empty : transactionReader.GetString(3)
                                };

                                dynamic param = JsonConvert.DeserializeObject(draw.Param);
                                object obj = param["BottomBound"];
                                obj.GetType();
                                points[0] = new Point3d(Convert.ToDouble(param[leftBound].Replace("_", ".")), Convert.ToDouble(param[bottomBound].Replace("_", ".")));
                                points[1] = new Point3d(Convert.ToDouble(param[rightBound].Replace("_", ".")), Convert.ToDouble(param[topBound].Replace("_", ".")));

                                if (!string.IsNullOrEmpty(windowVars.LayersTableName))
                                {
                                    draw.IsSimeSublayers = true;
                                    using (var layersReader = new OracleCommand($"SELECT layerName, sublayerName FROM {windowVars.LayersTableName} WHERE sublayerGUID = '{draw.SubleyerGUID}'", connection).ExecuteReader())
                                    {
                                        if (layersReader.Read())
                                        {
                                            draw.LayerName = layersReader.IsDBNull(0) ? string.Empty : layersReader.GetString(0);
                                            draw.SublayerName = layersReader.IsDBNull(1) ? string.Empty : layersReader.GetString(1);
                                        }
                                    }
                                }

                                return draw;
                            }
                            else
                            {
                                Thread.CurrentThread.Abort();
                                return null;
                            }
                        };
                        
                        break;
                    case XmlWindowVars windowVars:
                        conn = new XmlConnection(windowVars.GeometryPath);
                        XmlElement rowdata = conn.Connect();
                        if (rowdata is null)
                        {
                            MessageBox.Show("Нет данных для отрисовки!");
                        }
                        XmlNodeList rows = rowdata.SelectNodes("//ROW");

                        readAction = () =>
                        {
                            if (rows.GetEnumerator().MoveNext())
                            {
                                XmlNode row = rows.GetEnumerator().Current as XmlNode;
                                return new DrawParameters
                                {
                                    DrawSettings = JsonConvert.DeserializeObject<DrawSettings>(row.SelectSingleNode("DRAWJSON").InnerText),
                                    WKT = row.SelectSingleNode("GEOWKT").InnerText,
                                    SubleyerGUID = row.SelectSingleNode("SUBLAYERGUID").InnerText
                                };
                            }
                            else
                            {
                                return null;
                            }
                        };
                        break;
                    default: 
                        break;
                }

                int entitiesCount = 0;

                Action<DrawParameters> writeAction = (draw) =>
                {
                    entitiesCount++;

                    if (draw is null) return;

                    if (draw.SubleyerGUID != string.Empty)
                    {
                        if (draw.IsSimeSublayers && sublayer != draw.SubleyerGUID)
                        {
                            using (Transaction transaction = db.TransactionManager.StartTransaction())
                            {
                                layerTable = transaction.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                                string layerName = $"{draw.LayerName} | {draw.SublayerName}"; // i.ToString();
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

                            sublayer = draw.SubleyerGUID;
                        }

                        switch (draw.DrawSettings.GetDrawType)
                        {
                            case DrawType.Polyline: // Тип линии
                                {
                                    object geo = Aspose.Gis.Geometries.Geometry.FromText(draw.WKT);
                                    switch (geo)
                                    {
                                        case MultiLineString multiLine:
                                            {
                                                foreach (LineString line in multiLine)
                                                {
                                                    DrawLine(db, new Point3d[] 
                                                    { 
                                                        new Point3d(line[0].X, line[0].Y, line[0].Z), 
                                                        new Point3d(line[1].X, line[1].Y, line[1].Z), 
                                                    },
                                                    draw.DrawSettings);
                                                }
                                            }
                                            break;
                                        default: throw new Exception("Undefined geometry!");
                                    }
                                    //object obj = Reader.Read(draw.WKT, DrawType.Polyline);
                                    //if (obj is MultiLineStrings multi)
                                    //{
                                    //    if (multi.Lines.Count == 1)
                                    //    {
                                    //        DrawLine(db, multi.Lines[0].Points.ToArray(), draw.DrawSettings);
                                    //        if (GET_POINTS) Compare(points, multi.Lines[0].Points);
                                    //    }
                                    //    else
                                    //    {
                                    //        DrawMultiPolyline(db, multi, draw.DrawSettings, points, GET_POINTS);
                                    //    }
                                    //}
                                    //else if (obj is Polygon polygon)
                                    //{
                                    //    DrawPolygon(db, polygon, draw.DrawSettings, entitiesCount);
                                    //}
                                }
                                break;
                            case DrawType.LabelDrawParams: // Тип текст
                                {
                                    // object obj = Reader.Read(draw.WKT, DrawType.LabelDrawParams);
                                    using (var transaction = db.TransactionManager.StartTransaction())
                                    {
                                        var blockTable = transaction.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                                        var record = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                                        var text = new DBText();
                                        text.SetDatabaseDefaults();
                                        // LabelDrawSettings label = JsonConvert.DeserializeObject()
                                        text.Position = new Point3d(2, 0, 0);
                                        text.Height = 0.5;
                                        text.TextString = "Hello, world!";

                                        record.AppendEntity(text);
                                        transaction.AddNewlyCreatedDBObject(text, true);

                                        transaction.Commit();
                                    }
                                }
                                break;
                            case DrawType.Empty: // Пустое действие
                                break;
                            default: break;
                        }
                    }
                };

#if DEBUG
                StreamWriter writer = new StreamWriter("time.txt");
                writer.WriteLine($"Начало записи: {DateTime.Now}");
#endif

                var pipeline = new Pipeline<DrawParameters>(readAction, writeAction, limitItemsCount: 1);
                var thread = new Thread(pipeline.Run);

                thread.Start();
                thread.Join();
#if DEBUG
                writer.WriteLine($"Окончание записи: {DateTime.Now}");
                writer.Close();
                MessageBox.Show(pipeline.ReadedItemsCount.ToString());
#endif
                // Отрисовка рамки вокруг всех нарисованных объектов
                // if (GET_POINTS && !points.Equals(new Points())) DrawDebugObjects(db, points);

                // Zoom(new Point3d(points.X1, points.Y1, 0), new Point3d(points.X2, points.Y2, 0), new Point3d(), 1);
                Zoom(points[0], points[1], new Point3d(), 1);

                doc.Editor.WriteMessage("Закончена отрисовка объектов!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                conn?.Dispose();
                transactionReader?.Dispose();
                connection?.Dispose();
            }
        }
        */
        [CommandMethod("DrawAsync")]
        public async void DrawAsync()
        {
            string dbName = "k630f";
            DrawParameters draw;
            var doc = Application.DocumentManager.MdiActiveDocument;
            Point3d min = new Point3d(), max = new Point3d();
            Database db = doc.Database;
#if DEBUG
            const int limitEntities = 200;
            int counter = 0;
#endif
            using (var connection = new OracleConnection("Data Source=data-pc/GEO;Password=g;User Id=g;"))
            {
                await connection.OpenAsync();
                using (var reader = await new OracleCommand($"SELECT drawjson, geowkt, paramjson FROM {dbName}_trans_open", connection).ExecuteReaderAsync())
                {
#if DEBUG
                    while (reader.Read() && counter < limitEntities)
#else
                    while (reader.Read())
#endif
                    {
                        draw = new DrawParameters()
                        {
                            DrawSettings = JObject.Parse(reader.GetString(0))
                        };
                        if (draw.DrawSettings["DrawType"].ToString() != "Empty")
                        {
                            if (!reader.IsDBNull(1))
                            {
                                draw.WKT = reader.GetString(1);
                            }
                            else
                            {
#if DEBUG
                                counter++;
#endif
                                continue;
                            }
                            draw.Param = JObject.Parse(reader.GetString(2));
                        }
                        switch (draw.DrawSettings["DrawType"].ToString())
                        {
                            case "Polyline":
                                {
                                    // FIX: Неправильно задан Bounding Box
                                    object wkt = Aspose.Gis.Geometries.Geometry.FromText(draw.WKT);
                                    switch (wkt)
                                    {
                                        case MultiLineString multiLine:
                                            {
                                                foreach (LineString line in multiLine)
                                                {
                                                    DrawPolyline(db, line, draw.DrawSettings);
                                                }
                                            }
                                            break;
                                        case Polygon polygon:
                                            {
                                                DrawPolygon(db, polygon, draw.DrawSettings);
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                break;
                            case "TMMTTFSignDrawParams": // Знаки
                                break;
                            case "BasicSignDrawParams": // Знаки
                                break;
                            case "Empty":
                                break;
                            case "LabelDrawParams":
                                {
                                    object wkt = Aspose.Gis.Geometries.Geometry.FromText(draw.WKT);
                                    if (wkt is Aspose.Gis.Geometries.Point point)
                                    {
                                        DrawText(db, new Point3d(point.X, point.Y, 0), draw.DrawSettings["Text"].ToString(), Convert.ToInt32(draw.DrawSettings["FontSize"].ToString()));
                                    }
                                }
                                break;
                            default: break;
                        }
#if DEBUG
                        counter++;
#endif
                    }
                }
            }
            
            Zoom(min, max, new Point3d(), 1);
        }
        private void DrawText(Database db, Point3d position, string textString, int fontSize)
        {
            using (var transaction = db.TransactionManager.StartTransaction())
            {
                var blockTable = transaction.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                var record = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                var text = new DBText();
                text.SetDatabaseDefaults();
                text.Position = position;
                text.Height = fontSize;
                text.TextString = textString;

                record.AppendEntity(text);
                transaction.AddNewlyCreatedDBObject(text, true);

                transaction.Commit();
            }
        }
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
        private static void ShowError(string message) =>
            MessageBox.Show(message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
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
        private void DrawLine(Database db, Point3d[] points, JObject settings)
        {
            void action(Transaction transaction, BlockTableRecord record)
            {
                using (Line line = new Line(points[0], points[1]))
                {
                    line.Color = AColor.FromColor(SColor.FromArgb(Convert.ToInt32(settings["BrushColor"].ToString())));
                    line.Thickness = Convert.ToDouble(settings["Width"].ToString());

                    _ = record.AppendEntity(line);
                    transaction.AddNewlyCreatedDBObject(line, true);
                }
            }
            DrawEntity(db, action);
        }
        private void DrawPolyline(Database db, LineString line, JObject settings)
        {
            void action(Transaction transaction, BlockTableRecord record)
            {
                using (Polyline polyline = new Polyline())
                {
                    for (int i = 0; i < line.Count; i++)
                    {
                        polyline.AddVertexAt(i, new Point2d(line[i].X, line[i].Y), 0, 0, 0);
                    }
                    polyline.Color = AColor.FromColor(SColor.FromArgb(Convert.ToInt32(settings["BrushColor"].ToString())));
                    polyline.Thickness = Convert.ToInt32(settings["Width"].ToString());

                    record.AppendEntity(polyline);
                    transaction.AddNewlyCreatedDBObject(polyline, true);
                }
            }

            DrawEntity(db, action);
        }
        private void DrawPolygon(Database db, Polygon polygon, JObject settings)
        {
            using (var transaction = db.TransactionManager.StartTransaction())
            {
                using (var blockTable = transaction.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable)
                {
                    using (var record = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord)
                    {
                        var polyline = new Polyline();
                        var line = polygon.ReplacePolygonsByLines() as LineString;

                        for (int i = 0; i < line.Count; i++)
                        {
                            polyline.AddVertexAt(i, new Point2d(line[i].X, line[i].Y), 0, 0, 0);
                        }

                        polyline.Closed = true;
                        polyline.Color = AColor.FromColor(SColor.FromArgb(Convert.ToInt32(settings["BrushColor"].ToString())));
                        polyline.Thickness = Convert.ToInt32(settings["Width"].ToString());

                        record.AppendEntity(polyline);
                        transaction.AddNewlyCreatedDBObject(polyline, true);

                        var objCollection = new DBObjectCollection
                        {
                            polyline
                        };
                        var region = Region.CreateFromCurves(objCollection)[0] as Region;
                        region.Color = AColor.FromColor(SColor.FromArgb(Convert.ToInt32(settings["BrushBkColor"].ToString())));
                        record.AppendEntity(region);
                        transaction.AddNewlyCreatedDBObject(region, true);
                        
                        var objIdCollection = new ObjectIdCollection
                        {
                            region.ObjectId
                        };

                        using (var hatch = new Hatch())
                        {
                            record.AppendEntity(hatch);
                            transaction.AddNewlyCreatedDBObject(hatch, true);

                            hatch.SetHatchPattern(HatchPatternType.UserDefined, "SOLID");
                            hatch.Associative = true;
                            hatch.AppendLoop(HatchLoopTypes.Outermost, objIdCollection);
                            hatch.EvaluateHatch(true);
                        }

                        transaction.Commit();
                    }
                }
            }
        }
        public void Terminate() { }
    }
}