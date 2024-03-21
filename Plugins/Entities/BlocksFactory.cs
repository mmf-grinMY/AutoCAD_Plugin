using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;

using static Plugins.Constants;

namespace Plugins.Entities
{
    /// <summary>
    /// Создатель блоков
    /// </summary>
    sealed class BlocksFactory
    {
        #region Private Methods

        void Append(Transaction transaction, BlockTableRecord record, Autodesk.AutoCAD.DatabaseServices.Entity entity)
        {
            record.AppendEntity(entity);
            transaction.AddNewlyCreatedDBObject(entity, true);
        }
        void AddCircle(Transaction transaction, BlockTableRecord record, double radius, bool hasHatch = false)
        {
            var circle = new Circle()
            {
                Center = new Point3d(0, 0, 0),
                Radius = radius * SCALE,
                Color = Color.FromColorIndex(ColorMethod.ByBlock, 0)
            };

            Append(transaction, record, circle);

            if (hasHatch) AddHatch(transaction, record, circle);
        }
        void AddHatch(Transaction transaction, BlockTableRecord record, Autodesk.AutoCAD.DatabaseServices.Entity owner)
        {
            var hatch = new Hatch();

            Append(transaction, record, hatch);

            hatch.SetHatchPattern(HatchPatternType.UserDefined, "SOLID");
            hatch.Color = Color.FromColorIndex(ColorMethod.ByBlock, 0);
            hatch.Associative = true;
            hatch.AppendLoop(HatchLoopTypes.Outermost, new ObjectIdCollection { owner.ObjectId });
            hatch.EvaluateHatch(true);
        }
        void AddLine(Transaction transaction, BlockTableRecord record, Point3d p1, Point3d p2) => 
            Append(transaction, record, new Line(p1, p2) { Color = Color.FromColorIndex(ColorMethod.ByBlock, 0) });
        void AddPolygon(Transaction transaction, BlockTableRecord record, Point2d[] points)
        {
            var polyline = new Autodesk.AutoCAD.DatabaseServices.Polyline()
            {
                Color = Color.FromColorIndex(ColorMethod.ByBlock, 0)
            };

            for (int i = 0; i < points.Length; ++i)
            {
                polyline.AddVertexAt(i, new Point2d(points[i].X * SCALE, points[i].Y * SCALE), 0, 0, 0);
            }

            Append(transaction, record, polyline);
            AddHatch(transaction, record, polyline);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Создать блок
        /// </summary>
        /// <param name="blockName">Имя блока</param>
        /// <returns>true, если блок отрисован, false в противном случае</returns>
        public bool Create(string blockName)
        {
            var db = Application.DocumentManager.MdiActiveDocument.Database;

            using (var transaction = db.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                var record = new BlockTableRecord();

                table.Add(record);
                record.Name = blockName;

                transaction.AddNewlyCreatedDBObject(record, true);

                switch (blockName)
                {
                    // TODO: Сделать создание блоков по описанию в файле
                    case "pnt!.chr_48":
                        AddCircle(transaction, record, 2);
                        AddCircle(transaction, record, 4);
                        transaction.Commit();
                        break;
                    case "pnt!.chr_53":
                        AddCircle(transaction, record, 2, true);
                        transaction.Commit();
                        break;
                    case "pnt!.chr_100":
                        AddCircle(transaction, record, 3);
                        AddLine(transaction, record, new Point3d(-1 * SCALE, 0, 0), new Point3d(1 * SCALE, 0, 0));
                        AddLine(transaction, record, new Point3d(0, -1 * SCALE, 0), new Point3d(0, 1 * SCALE, 1 * SCALE));
                        transaction.Commit();
                        break;
                    case "pnt!.chr_117":
                        AddPolygon(transaction, record, new Point2d[] { new Point2d(-3, -3), new Point2d(3, -3), new Point2d(0, 4) });
                        transaction.Commit();
                        break;
                    case "pnt!.chr_123":
                        AddPolygon(transaction, record, new Point2d[] { new Point2d(-3, 3), new Point2d(3, 3), new Point2d(0, -4) });
                        transaction.Commit();
                        break;
                    case "pnt!.chr_139":
                        AddCircle(transaction, record, 3);
                        transaction.Commit();
                        break;
                    default: return false;
                }
            }

            return true;
        }

        #endregion
    }
}