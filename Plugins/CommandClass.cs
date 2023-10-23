using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Xml;
using Plugins.WKT;
using Newtonsoft.Json;
using Line = Autodesk.AutoCAD.DatabaseServices.Line;
using Polygon = Plugins.WKT.Polygon;
using SColor = System.Drawing.Color;
using Exception = System.Exception;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using AColor = Autodesk.AutoCAD.Colors.Color;
using System.Drawing;
using Oracle.ManagedDataAccess.Client;
using System.Runtime;

namespace Plugins
{
    public enum DataSource
    {
        OracleDatabase,
        XmlDocument
    }
    public class Commands : IExtensionApplication
    {
        public void Initialize()
        {
            try
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc is null) throw new ArgumentNullException(nameof(doc));
                var editor = doc.Editor;
                string helloMessage = "Загрузка плагина прошла успешно!";
                editor.WriteMessage(helloMessage);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла ошибка!\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public static List<DrawParameters> LoadDataFromDB(object tuple)
        {
            string user, password, host, privilege;
            (user, password, host, privilege) = tuple as Tuple<string, string, string, string>;
            MessageBox.Show(password);
            string conStringUser = $"Data Source={host};Password={password};User Id={user};";
            conStringUser += privilege == "NORMAL" ? string.Empty : $"DBA Privilege = {privilege};";
            List<DrawParameters> drawParameters = new List<DrawParameters>();
            using (OracleConnection connection = new OracleConnection(conStringUser))
            {
                if (connection is null) throw new ArgumentNullException(nameof(connection));
                connection.Open();
                string sqlExpression = "select drawjson, geowkt, SUBLAYERGUID from K670F_TRANS_OPEN";
                var cmd = connection.CreateCommand();
                cmd.CommandText = sqlExpression;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DrawParameters parameters = new DrawParameters
                        {
                            DrawSettings = reader.IsDBNull(0) ? DrawSettings.Empty : JsonConvert.DeserializeObject<DrawSettings>(reader.GetString(0)),
                            WKT = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            SubleyerGUID = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        };
                        drawParameters.Add(parameters);
                    }
                }
            }
            return drawParameters;
        }
        public static List<DrawParameters> LoadDataFromXml(object tuple)
        {
            List<DrawParameters> drawParameters = new List<DrawParameters>();
            string fileGeometry, fileLayers;
            (fileGeometry, fileLayers) = tuple as Tuple<string, string>;
            using (XmlConnection connection = new XmlConnection(fileGeometry))
            {
                XmlElement rowdata = connection.Connect();
                if (rowdata is null) return drawParameters;
                XmlNodeList rows = rowdata.SelectNodes("//ROW");
                if (rows != null)
                {
                    foreach (XmlNode row in rows)
                    {
                        DrawParameters parameters = new DrawParameters
                        {
                            DrawSettings = JsonConvert.DeserializeObject<DrawSettings>(row.SelectSingleNode("DRAWJSON").InnerText),
                            WKT = row.SelectSingleNode("GEOWKT").InnerText,
                            SubleyerGUID = row.SelectSingleNode("SUBLAYERGUID").InnerText
                        };
                        drawParameters.Add(parameters);
                    }
                }
            }
            return drawParameters;
        }
        class Points
        {
            public Points()
            {
                X0 = Y0 = double.MaxValue;
                Y1 = X1 = 0;
            }
            public double X0 { get; set; }
            public double Y0 { get; set; }
            public double Y1 { get; set; }
            public double X1 { get; set; }
        }
        [CommandMethod("Draw")]
        public void Draw()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc is null) throw new ArgumentNullException(nameof(doc));
            Database db = doc.Database;
            DataSource dataSource;
            List<DrawParameters> drawParameters;
            try
            {
                var window = new LoginWindow();
                bool? resultDialog = window.ShowDialog();
                object tuple;

                if (resultDialog.HasValue)
                {
                    (dataSource, tuple) = window.Vars;
                }
                else
                {
                    MessageBox.Show("Для отрисовки объектов требуются данные!");
                    return;
                }

                switch (dataSource)
                {
                    case DataSource.OracleDatabase: drawParameters = LoadDataFromDB(tuple); break;
                    case DataSource.XmlDocument: drawParameters = LoadDataFromXml(tuple); break;
                    default: drawParameters = new List<DrawParameters>(); break;
                }
            
                #region Get rechtangle coords

                bool debug = true;
                Points points = new Points();

                int i = 1;
                string sublayer = string.Empty;
                LayerTable layerTable;
                HashSet<string> sublayers = new HashSet<string>();
                foreach (var draw in drawParameters)
                {
                    if (draw.SubleyerGUID != string.Empty)
                    {
                        if (sublayer != draw.SubleyerGUID)
                        {
                            using (Transaction transaction = db.TransactionManager.StartTransaction())
                            {
                                layerTable = transaction.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                            
                                if (!sublayers.Contains(draw.SubleyerGUID))
                                {
                                    string layerName = i.ToString();
                                    if (layerTable.Has(layerName) == false)
                                    {
                                        LayerTableRecord layerTableRecord = new LayerTableRecord { Name = layerName };
                                        layerTable.UpgradeOpen();
                                        layerTable.Add(layerTableRecord);
                                        transaction.AddNewlyCreatedDBObject(layerTableRecord, true);
                                        db.Clayer = layerTable[layerName];
                                        i++;
                                    }
                                    sublayers.Add(draw.SubleyerGUID);
                                }

                                transaction.Commit();
                            }

                            sublayer = draw.SubleyerGUID;
                        }

                        switch (draw.DrawSettings.GetDrawType)
                        {
                            case DrawType.Polyline:
                                object obj = Reader.Read(draw.WKT, DrawType.Polyline);
                                if (obj is MultiLineStrings multi)
                                {
                                    if (multi.Lines.Count == 1)
                                    {
                                        DrawLine(db, multi.Lines[0].Points.ToArray(), draw.DrawSettings);
                                        if (debug) Compare(points, multi.Lines[0].Points);
                                    }
                                    else
                                    {
                                        DrawMultiPolyline(db, multi, draw.DrawSettings, points, debug);
                                    }
                                }
                                else if (obj is Polygon polygon)
                                {
                                    foreach (LineString line in polygon.Lines)
                                    {
                                        DrawPolyline(db, line, draw.DrawSettings);
                                        if (debug) Compare(points, line.Points);
                                    }
                                }
                                break;
                            default: break;
                        }
                    }
                }

                #endregion

                if (debug) DrawDebugObjects(db, points);
                doc.Editor.WriteMessage("Закончена отрисовка объектов!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void DrawDebugObjects(Database db, Points p)
        {
            double padding = 100.0;
            p.X0 -= padding;
            p.X1 += padding;
            p.Y0 -= padding;
            p.Y1 += padding;
            Polygon ramka = new Polygon(string.Format("POLYGON(({0} {1}, {0} {3}, {2} {3}, {2} {1}, {0} {1}))", p.X0, p.Y0, p.X1, p.Y1));
            DrawSettings settings = new DrawSettings() { BrushColor = 0x00FFCC };

            using(Transaction transaction = db.TransactionManager.StartTransaction())
            {
                LayerTable layerTable = layerTable = transaction.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                string layerName = "DEBUG LAYER";
                if (layerTable.Has(layerName) == false)
                {
                    LayerTableRecord layerTableRecord = new LayerTableRecord
                    {
                        Name = layerName
                    };
                    layerTable.UpgradeOpen();
                    layerTable.Add(layerTableRecord);
                    transaction.AddNewlyCreatedDBObject(layerTableRecord, true);

                    db.Clayer = layerTable[layerName];

                    transaction.Commit();
                }
            }

            foreach (LineString line in ramka.Lines) DrawPolyline(db, line, settings);
            DrawLine(db, new Point[] { new Point(0, 0), new Point(p.X0, p.Y0) }, settings);
        }
        private void Compare(Points p, List<Point> points)
        {
            foreach (Point point in points)
            {
                p.X1 = Math.Max(p.X1, point.X);
                p.Y1 = Math.Max(p.Y1, point.Y);
                p.X0 = Math.Min(p.X0, point.X);
                p.Y0 = Math.Min(p.Y0, point.Y);
            }
        }
        private void DrawEntity(Database db, Action<Transaction, BlockTableRecord> action)
        {
            Transaction transaction = null;
            try
            {
                transaction = db.TransactionManager.StartTransaction();
                if (transaction is null) throw new ArgumentNullException(nameof(transaction));
                if (!(transaction.GetObject(db.BlockTableId, OpenMode.ForRead) is BlockTable blockTable)) throw new ArgumentNullException(nameof(blockTable));
                if (!(transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) is BlockTableRecord record)) throw new ArgumentNullException(nameof(record));
                action(transaction, record);
                transaction.Commit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла ошибка!\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                transaction?.Dispose();
            }
        }
        private void DrawLine(Database db, Point[] points, DrawSettings settings)
        {
            void action(Transaction transaction, BlockTableRecord record)
            {
                using (Line line = new Line(points[0].ToPoint3d(), points[1].ToPoint3d()))
                {
                    line.Color = AColor.FromColor(SColor.FromArgb(settings.BrushColor));
                    line.Thickness = settings.Width;

                    _ = record.AppendEntity(line);
                    transaction.AddNewlyCreatedDBObject(line, true);
                }
            }
            DrawEntity(db, action);
        }
        private void DrawPolyline(Database db, LineString line, DrawSettings settings)
        {
            void action(Transaction transaction, BlockTableRecord record)
            {
                var points = line.Points;
                using (Polyline polyline = new Polyline())
                {
                    for (int i = 0; i < points.Count; i++)
                    {
                        polyline.AddVertexAt(i, points[i].ToPoint2d(), 0, 0, 0);
                    }
                    polyline.Color = AColor.FromColor(SColor.FromArgb(settings.BrushColor));
                    polyline.Thickness = settings.Width;

                    record.AppendEntity(polyline);
                    transaction.AddNewlyCreatedDBObject(polyline, true);
                }
            }

            DrawEntity(db, action);
        }
        private void DrawMultiPolyline(Database db, MultiLineStrings multi, DrawSettings settings, Points p, bool debug)
        {
            foreach (LineString line in multi.Lines)
            {
                void action(Transaction transaction, BlockTableRecord record)
                {
                    var points = line.Points;
                    using (Polyline polyline = new Polyline())
                    {
                        for (int i = 0; i < points.Count; i++)
                        {
                            polyline.AddVertexAt(i, points[i].ToPoint2d(), 0, 0, 0);
                        }
                        polyline.Color = AColor.FromColor(SColor.FromArgb(settings.BrushColor));
                        polyline.Thickness = settings.Width;

                        record.AppendEntity(polyline);
                        transaction.AddNewlyCreatedDBObject(polyline, true);
                    }
                }

                DrawEntity(db, action);
                if (debug) Compare(p, line.Points);
            }
        }
        public void Terminate() { }
    }
}