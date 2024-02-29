#define MY_BOUNDING_BOX

using Plugins.Entities;

using System.Windows;
using System;

using AApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using ALine = Autodesk.AutoCAD.DatabaseServices.Line;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace Plugins
{
    /// <summary>
    /// Диспетчер управления отрисовкой примитивов
    /// </summary>
    internal class ObjectDispatcher
    {
        #region Private Fields

        private readonly EntitiesFactory factory;
        /// <summary>
        /// Рисуемый горизонт
        /// </summary>
        private readonly string gorizont;
        /// <summary>
        /// Менеджер поключения к Oracle БД
        /// </summary>
        private readonly OracleDbDispatcher connection;
        /// <summary>
        /// Внутренняя БД AutoCAD
        /// </summary>
        private static readonly Database db;
#if MY_BOUNDING_BOX
        readonly Box box;
#endif

        #endregion
        
        #region Private Static Methods

        /// <summary>
        /// Прибилизить к рамке
        /// </summary>
        /// <param name="min">Минимальная точка</param>
        /// <param name="max">Максимальная точка</param>
        /// <param name="center">Центр рамки</param>
        /// <param name="factor">Масштаб приближения</param>
        public static void Zoom(Point3d min, Point3d max, Point3d center, double factor)
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

        #endregion

        #region Private Methods

        /// <summary>
        /// Создать новый слой
        /// </summary>
        /// <param name="layerName">Имя слоя</param>
        private void CreateLayer(string layerName)
        {
            using (var transaction = db.TransactionManager.StartTransaction())
            {
                using (var table = transaction.GetObject(db.LayerTableId, OpenMode.ForWrite) as LayerTable)
                {
                    using (var record = new LayerTableRecord { Name = layerName })
                    {
                        table.Add(record);
                        transaction.AddNewlyCreatedDBObject(record, true);
                    }
                }

                transaction.Commit();
            }
        }

        #endregion

        #region Ctors

        /// <summary>
        /// Статическое создание
        /// </summary>
        static ObjectDispatcher()
        {
            db = AApplication.DocumentManager.MdiActiveDocument.Database;
        }
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="connection">Менеджер подключения</param>
        /// <param name="selectedGorizont">Выбранный горизонт</param>
        public ObjectDispatcher(OracleDbDispatcher connection, string selectedGorizont)
        {
            this.connection = connection;
            gorizont = selectedGorizont;
            box = new Box() { Bottom = long.MaxValue, Left = long.MaxValue, Right = long.MinValue, Top = long.MinValue };
            factory = new EntitiesFactory(AApplication.DocumentManager.MdiActiveDocument.Database, box);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Начать отрисовку объектов
        /// </summary>
        /// <param name="window">Окно отображения пргресса отрисовки</param>
        public void Draw()
        {
            var layersCache = new HashSet<string>();

            using (var reader = connection.GetDrawParams(gorizont))
            {
                // TODO: Добавить индикатор прогресса
                while(reader.Read())
                {
                    try
                    {
                        var draw = new Primitive(reader["geowkt"].ToString(),
                                                  reader["drawjson"].ToString(),
                                                  reader["paramjson"].ToString(),
                                                  reader["layername"] + " | " + reader["sublayername"],
                                                  reader["systemid"].ToString(),
                                                  reader["basename"].ToString(),
                                                  reader["childfields"].ToString());

                        var layer = draw.LayerName;

                        if (!layersCache.Contains(layer)) 
                        {
                            layersCache.Add(layer);
                            CreateLayer(layer);
                        }
                        
                        using (var entity = factory.Create(draw))
                        {
                            entity?.Draw();
                        }
                    }
                    catch (FormatException)
                    {
                        var rows = new string[]
                        {
                            "geowkt",
                            "drawjson",
                            "paramjson",
                            "layername",
                            "sublayername",
                            "systemid",
                            "basename",
                            "childfields"
                        };

                        foreach (var row in rows) {
                            if (reader[row] == null)
                                MessageBox.Show("Столбец " + row + " принимает значение NULL!" + '\n' + reader["geowkt"]);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.GetType() + "\n" + ex.Message + "\n" + ex.StackTrace + "\n" + ex.Source);
                    }
                }
            }
            MessageBox.Show("Закончена отрисовка геометрии!");
            Zoom(new Point3d(box.Left, box.Bottom, 0), new Point3d(box.Right, box.Top, 0), new Point3d(0, 0, 0), 1.0);
        }

        #endregion
    }
}