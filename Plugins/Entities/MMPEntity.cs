#define MY_BOUNDING_BOX

using System;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace Plugins
{
    abstract class MMPEntity
    {
        private readonly Box box;
        protected readonly int scale = 1_000;
        protected readonly Database db;
        protected readonly DrawParams drawParams;
#if MY_BOUNDING_BOX
        /// <summary>
        /// Проверить двумерную точку на принадлежность рамке
        /// </summary>
        /// <param name="point">Проверяемая точка</param>
        protected void CheckBoundingBox(Point2d point)
        {
            box.Left = Convert.ToInt64(Math.Min(point.X, box.Left));
            box.Right = Convert.ToInt64(Math.Max(point.X, box.Right));
            box.Bottom = Convert.ToInt64(Math.Min(point.Y, box.Bottom));
            box.Top = Convert.ToInt64(Math.Max(point.Y, box.Top));
        }
        /// <summary>
        /// Проверить трехмерную точку на принадлежность рамке
        /// </summary>
        /// <param name="point">Проверяемая точка</param>
        protected void CheckBoundingBox(Point3d point)
        {
            box.Left = Convert.ToInt64(Math.Min(point.X, box.Left));
            box.Right = Convert.ToInt64(Math.Max(point.X, box.Right));
            box.Bottom = Convert.ToInt64(Math.Min(point.Y, box.Bottom));
            box.Top = Convert.ToInt64(Math.Max(point.Y, box.Top));
        }
#endif
        public MMPEntity(Database db, DrawParams drawParams, Box box)
        {
            this.db = db;
            this.drawParams = drawParams;
            this.box = box;
        }
        public abstract void DrawLogic(Transaction transaction, BlockTableRecord record);
        public void Draw()
        {
            using (var transaction = db.TransactionManager.StartTransaction())
            {
                using (var blockTable = transaction.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable)
                {
                    using (var record = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord)
                    {
                        DrawLogic(transaction, record);
                        transaction.Commit();
                    }
                }
            }
        } 
    }
}