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
        /// Транзакция отрисовки объекта
        /// </summary>
        readonly Transaction transaction;
        /// <summary>
        /// Запись объекта отрисовки в таблицу
        /// </summary>
        readonly BlockTableRecord record;
        /// <summary>
        /// Таблица записей примитивов
        /// </summary>
        readonly BlockTable table;

        #endregion

        #region Protected Fields

        /// <summary>
        /// Ключевое слово
        /// </summary>
        protected readonly string COLOR = "Color";
        /// <summary>
        /// Параметры отрисовки объекта
        /// </summary>
        protected readonly Primitive primitive;

        #endregion

        #region Ctors

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="db">Внутренняя база данных AutoCAD</param>
        /// <param name="prim">Параметры отрисовки объекта</param>
        /// <param name="box">Общий BoundingBox рисуемых объектов</param>
        public Entity(Database db, Primitive prim)
        {
            transaction = db.TransactionManager.StartTransaction();
            table = transaction.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
            record = transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
            primitive = prim;
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
            entity.AddXData(primitive);
            record.AppendEntity(entity);
            transaction.AddNewlyCreatedDBObject(entity, true);
            Session.CheckBounds(entity);
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
        public bool HasKey(string key) => table.Has(key);
        /// <summary>
        /// Изъятие Id объекта
        /// </summary>
        /// <param name="key">Ключ объекта</param>
        /// <returns>Id искомого объекта</returns>
        public ObjectId GetByKey(string key) => table[key];

        #endregion
    }
}