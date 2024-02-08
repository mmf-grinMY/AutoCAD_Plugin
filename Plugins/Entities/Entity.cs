using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Diagnostics;

namespace Plugins.Entities
{
    /// <summary>
    /// Объект отрисовки
    /// </summary>
    abstract class Entity
    {
        #region Private Fields
        /// <summary>
        /// Пустой объект
        /// </summary>
        public static Entity Empty => null;
#if DEBUG
        /// <summary>
        /// Счетчик ошибок при отрисовке
        /// </summary>
        protected readonly ErrorCounter counter;
#endif
        /// <summary>
        /// BoundingBox
        /// </summary>
        protected readonly Box box;
        /// <summary>
        /// Внутренняя база данных AutoCAD
        /// </summary>
        protected readonly Database db;
        /// <summary>
        /// Параметры отрисовки объекта
        /// </summary>
        protected readonly DrawParams drawParams;
        #endregion
        #region Ctors
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="db">Внутренняя база данных AutoCAD</param>
        /// <param name="drawParams">Параметры отрисовки объекта</param>
        /// <param name="box">Общий BoundingBox рисуемых объектов</param>
        /// <param name="counter">Счетчик ошибок</param>
        public Entity(Database db, DrawParams drawParams, Box box
#if DEBUG
            , ErrorCounter counter
#endif
            )
        {
            this.db = db;
            this.drawParams = drawParams;
            this.box = box;
#if DEBUG
            this.counter = counter;
#endif
        }
        #endregion
        #region Protected Methods
        /// <summary>
        /// Внутренняя логика отрисовки объекта
        /// </summary>
        /// <param name="transaction">Транзакция внутренней базы данных AutoCAD</param>
        /// <param name="record">Запись таблицы блоков</param>
        protected abstract void DrawLogic(Transaction transaction, BlockTableRecord record);
        /// <summary>
        /// Перепроверить BoundingBox
        /// </summary>
        /// <param name="entity"></param>
        protected void CheckBounds(Autodesk.AutoCAD.DatabaseServices.Entity entity)
        {
            if (entity != null && entity.Bounds != null)
            {
                box.Left = Convert.ToInt64(Math.Min(box.Left, entity.Bounds.Value.MinPoint.X));
                box.Right = Convert.ToInt64(Math.Max(box.Right, entity.Bounds.Value.MaxPoint.X));
                box.Bottom = Convert.ToInt64(Math.Min(box.Bottom, entity.Bounds.Value.MinPoint.Y));
                box.Top = Convert.ToInt64(Math.Max(box.Top, entity.Bounds.Value.MaxPoint.Y));
            }
        }
        #endregion
        #region Public Methods
        /// <summary>
        /// Нарисовать объект
        /// </summary>
        public void Draw()
        {
#if DEBUG
            counter.StartObjectDraw();
#endif
            using (var transaction = db.TransactionManager.StartTransaction())
            {
                using (var blockTable = transaction.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable)
                {
                    using (var record = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord)
                    {
                        DrawLogic(transaction, record);
#if DEBUG
                        counter.EndObjectDraw();
#endif
                        transaction.Commit();
                    }
                }
            }
        }
#endregion
    }
}