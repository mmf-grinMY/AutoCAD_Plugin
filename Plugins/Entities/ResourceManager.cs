using System;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;

using static Plugins.Constants;

// TODO: Убрать этот класс
namespace Plugins.Entities
{
    /// <summary>
    /// Управляющий ресурсами создателя блоков
    /// </summary>
    class ResourceManager : IDisposable
    {
        #region Private Fields
        /// <summary>
        /// Должен быть остановлен
        /// </summary>
        private bool isMustAborted;
        /// <summary>
        /// Транзакция во внутреннюю базу данных AutoCAD
        /// </summary>
        private readonly Transaction transaction;
        /// <summary>
        /// Запись в таблицу блоков
        /// </summary>
        private BlockTableRecord record;
        /// <summary>
        /// Внутренняя база данных AutoCAD
        /// </summary>
        private readonly Database db;
        #endregion

        #region Ctors
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="db">Внутренняя база данных AutoCAD</param>
        public ResourceManager(Database database)
        {
            db = database;
            transaction = db.TransactionManager.StartTransaction();
            isMustAborted = false;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Имя создаваемого блока
        /// </summary>
        public string Name 
        { 
            set
            {
                var blockTable = transaction.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;

                record = new BlockTableRecord();
                blockTable.Add(record);
                record.Name = value;
                transaction.AddNewlyCreatedDBObject(record, true);
            } 
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Сохрнаить объект отрисовки
        /// </summary>
        /// <param name="entity"></param>
        public void Append(Autodesk.AutoCAD.DatabaseServices.Entity entity)
        {
            record.AppendEntity(entity);
            transaction.AddNewlyCreatedDBObject(entity, true);
        }
        /// <summary>
        /// Не сохранять блок
        /// </summary>
        public void Abort()
        {
            isMustAborted = true;
        }
        /// <summary>
        /// Освободить занятые ресурсы
        /// </summary>
        public void Dispose()
        {
            if (!isMustAborted) 
                transaction.Commit();
            record.Dispose();
            transaction.Dispose();
        }
        /// <summary>
        /// Добавить линию
        /// </summary>
        /// <param name="p1">Начальная точка линии</param>
        /// <param name="p2">Конечная точка линии</param>
        public void AddLine(Point3d p1, Point3d p2)
        {
            using (var line = new Line(p1, p2))
            {
                line.Color = Color.FromColorIndex(ColorMethod.ByBlock, 0);

                Append(line);
            }
        }
        /// <summary>
        /// Добавить круг
        /// </summary>
        /// <param name="radius">Радиус круга</param>
        /// <param name="isHatched">Заштрихован</param>
        public void AddCircle(double radius, bool isHatched = false)
        {
            using (var circle = new Circle())
            {
                circle.SetDatabaseDefaults();
                circle.Center = new Point3d(0, 0, 0);
                circle.Radius = radius * SCALE;
                circle.Color = Color.FromColorIndex(ColorMethod.ByBlock, 0);

                Append(circle);

                if (isHatched)
                {
                    var objIdCollection = new ObjectIdCollection { circle.ObjectId };

                    using (var hatch = new Hatch())
                    {
                        Append(hatch);

                        hatch.SetHatchPattern(HatchPatternType.UserDefined, "SOLID");
                        hatch.Color = Color.FromColorIndex(ColorMethod.ByBlock, 0);
                        hatch.Associative = true;
                        hatch.AppendLoop(HatchLoopTypes.Outermost, objIdCollection);
                        hatch.EvaluateHatch(true);
                    }
                }
            }
        }
        /// <summary>
        /// Добавить полигон
        /// </summary>
        /// <param name="points">Вершины полигона</param>
        public void AddPolygon(Point2d[] points)
        {
            using (var polyline = new Autodesk.AutoCAD.DatabaseServices.Polyline())
            {
                for (int i = 0; i < points.Length; ++i)
                {
                    polyline.AddVertexAt(i, new Point2d(points[i].X * SCALE, points[i].Y * SCALE), 0, 0, 0);
                }
                polyline.Closed = true;
                polyline.Color = Color.FromColorIndex(ColorMethod.ByBlock, 0);

                Append(polyline);

                var objIdCollection = new ObjectIdCollection { polyline.ObjectId };

                using (var hatch = new Hatch())
                {
                    Append(hatch);

                    hatch.SetHatchPattern(HatchPatternType.UserDefined, "SOLID");
                    hatch.Color = Color.FromColorIndex(ColorMethod.ByBlock, 0);
                    hatch.Associative = true;
                    hatch.AppendLoop(HatchLoopTypes.Outermost, objIdCollection);
                    hatch.EvaluateHatch(true);
                }
            }
        }
        #endregion
    }
}