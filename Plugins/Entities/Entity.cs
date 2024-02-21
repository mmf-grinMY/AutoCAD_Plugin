using System;

using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins.Entities
{
    /// <summary>
    /// Объект отрисовки
    /// </summary>
    abstract class Entity : IDisposable
    {
        #region Private Fields
        /// <summary>
        /// BoundingBox
        /// </summary>
        private readonly Box box;
        /// <summary>
        /// Параметры отрисовки объекта
        /// </summary>
        protected readonly DrawParams drawParams;
        private readonly Transaction transaction;
        private readonly BlockTableRecord record;
        private readonly BlockTable table;
        protected readonly string COLOR = "Color";
        #endregion
        #region Ctors
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="db">Внутренняя база данных AutoCAD</param>
        /// <param name="drawParams">Параметры отрисовки объекта</param>
        /// <param name="box">Общий BoundingBox рисуемых объектов</param>
        /// <param name="counter">Счетчик ошибок</param>
        public Entity(Database db, DrawParams drawParams, Box box)
        {
            transaction = db.TransactionManager.StartTransaction();
            table = transaction.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
            record = transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
            this.drawParams = drawParams;
            this.box = box;
        }
        #endregion
        #region Protected Methods
        protected void AppendToDb(Autodesk.AutoCAD.DatabaseServices.Entity entity)
        {
            entity.SetDatabaseDefaults();
            entity.AddXData(drawParams);
            record.AppendEntity(entity);
            transaction.AddNewlyCreatedDBObject(entity, true);
            CheckBounds(entity);
        }
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
        public virtual void Draw() { }
        public void Dispose()
        {
            transaction.Commit();
            record.Dispose();
            table.Dispose();
            transaction.Dispose();
        }
        public bool HasKey(string key)
        {
            return table.Has(key);
        }
        public ObjectId GetByKey(string key)
        {
            return table[key];
        }
        #endregion
    }
}