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
        protected readonly Primitive drawParams;
        /// <summary>
        /// Транзакция отрисовки объекта
        /// </summary>
        private readonly Transaction transaction;
        /// <summary>
        /// Запись объекта отрисовки в таблицу
        /// </summary>
        private readonly BlockTableRecord record;
        /// <summary>
        /// Таблица записей примитивов
        /// </summary>
        private readonly BlockTable table;
        #endregion

        #region Protected Fields
        /// <summary>
        /// Ключевое слово
        /// </summary>
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
        public Entity(Database db, Primitive drawParams, Box box)
        {
            transaction = db.TransactionManager.StartTransaction();
            table = transaction.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
            record = transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
            this.drawParams = drawParams;
            this.box = box;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Запись объекта в БД
        /// </summary>
        /// <param name="entity">Объект для записи</param>
        protected void AppendToDb(Autodesk.AutoCAD.DatabaseServices.Entity entity)
        {
            entity.SetDatabaseDefaults();
            entity.AddXData(drawParams);
            record.AppendEntity(entity);
            transaction.AddNewlyCreatedDBObject(entity, true);
            CheckBounds(entity);
        }
        /// <summary>
        /// Перепроверка BoundingBox
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
        /// Рисование объекта
        /// </summary>
        public virtual void Draw() { }
        /// <summary>
        /// Освобождение ресурсов объекта
        /// </summary>
        public void Dispose()
        {
            transaction.Commit();
            record.Dispose();
            table.Dispose();
            transaction.Dispose();
        }
        /// <summary>
        /// Проверка на наличие ключа
        /// </summary>
        /// <param name="key">Проверяемый ключ</param>
        /// <returns>true, если ключ имеется, false в противном случае</returns>
        public bool HasKey(string key)
        {
            return table.Has(key);
        }
        /// <summary>
        /// Изъятие Id объекта
        /// </summary>
        /// <param name="key">Ключ объекта</param>
        /// <returns>Id искомого объекта</returns>
        public ObjectId GetByKey(string key)
        {
            return table[key];
        }
        #endregion
    }
}