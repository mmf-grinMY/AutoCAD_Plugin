using Plugins.Logging;

using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins.Entities
{
    /// <summary>
    /// Объект отрисовки
    /// </summary>
    abstract class Entity
    {
        #region Public Fields

        /// <summary>
        /// Параметры отрисовки объекта
        /// </summary>
        public readonly Primitive primitive;

        #endregion

        #region Ctor

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="primitive">Параметры отрисовки объекта</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public Entity(Primitive primitive) =>
            this.primitive = primitive ?? throw new System.ArgumentNullException(nameof(primitive));

        #endregion

        #region Protected Methods

        /// <summary>
        /// Логика отрисовки примитива
        /// </summary>
        /// <param name="transaction">Текщуая транзакия в БД AutoCAD</param>
        /// <param name="table">Таблица блоков</param>
        /// <param name="record">Текущая запись в таблицу блоков</param>
        /// <param name="logger">Логер событий</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        protected virtual void Draw(Transaction transaction, BlockTable table, BlockTableRecord record, ILogger logger)
        {
            if (transaction is null)
                throw new System.ArgumentNullException(nameof(transaction));

            if (table is null)
                throw new System.ArgumentNullException(nameof(table));

            if (record is null)
                throw new System.ArgumentNullException(nameof(record));

            if (logger is null)
                throw new System.ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Рисование объекта
        /// </summary>
        /// <param name="db">Текущая БД AutoCAD</param>
        /// <param name="logger">Логер событий</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public void AppendToDrawing(Database db, ILogger logger) 
        {
            if (db is null)
                throw new System.ArgumentNullException(nameof(db));

            if (logger is null)
                throw new System.ArgumentNullException(nameof(logger));

            using (var transaction = new MyTransaction(db, logger))
            {
                transaction.AddBlockRecord(Draw);
            }
        }

        #endregion
    }
    /// <summary>
    /// Стилизированный объект отрисовки
    /// </summary>
    abstract class StyledEntity : Entity
    {
        #region Protected Fields

        /// <summary>
        /// Стиль отрисовки
        /// </summary>
        protected readonly MyEntityStyle style;

        #endregion

        #region Ctor

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="primitive">Рисуемый примитив</param>
        /// <param name="style">Стиль отрисовки</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public StyledEntity(Primitive primitive, MyEntityStyle style) : base(primitive)
            => this.style = style ?? throw new System.ArgumentNullException(nameof(style));

        #endregion
    }
}