#define MY_BOUNDING_BOX

using Plugins.Entities;

using System.Windows;
using System;

using Oracle.ManagedDataAccess.Client;

using AApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using ALine = Autodesk.AutoCAD.DatabaseServices.Line;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

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
        private readonly OracleDbDispatcher connection;
        private readonly Database db;
        /// <summary>
        /// Текущий слой
        /// </summary>
        private string currentLayer = string.Empty;
#if MY_BOUNDING_BOX
        readonly Box box;
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

        #endregion

        #region Private Methods

        /// <summary>
        /// Создать новый слой
        /// </summary>
        /// <param name="layerName">Имя слоя</param>
        private void CreateLayer(string layerName)
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
                {
                    return new Draw(reader.GetString(1),
                                    reader.GetString(0),
                                    reader.GetString(2),
                                    $"{reader.GetString(3)} | {reader.GetString(4)}",
                                    reader.GetString(5),
                                    null);
                }
                else
                {
                    return new Draw(reader.GetString(1),
                                    reader.GetString(0),
                                    reader.GetString(2),
                                    $"{reader.GetString(3)} | {reader.GetString(4)}",
                                    reader.GetString(5),
                                    new LinkedDBFields(reader.GetString(6), reader.GetString(7)));
                }
            }

            return new Draw();
        }
        #endregion

        #region Ctors
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="connection">Менеджер подключения</param>
        /// <param name="selectedGorizont">Выбранный горизонт</param>
        public ObjectDispatcher(OracleDbDispatcher connection, string selectedGorizont)
        {
            this.connection = connection;
            gorizont = selectedGorizont;
            db = AApplication.DocumentManager.MdiActiveDocument.Database;
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
            using (var reader = connection.GetDrawParams(gorizont))
            {
                while(reader.Read())
                {
                    try
                    {
                        var param = Read(reader);
                        var draw = Sort(param);
                        CreateLayer(draw.LayerName);
                        using (var entity = factory.Create(draw))
                        {
                            entity?.Draw();
                        }
                    }
                    catch (GotoException)
                    {
                        continue;
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