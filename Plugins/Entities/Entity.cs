using Plugins.Logging;

using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins.Entities
{
    /// <summary>
    /// Объект отрисовки
    /// </summary>
    abstract class Entity
    {
        #region Protected Fields

        /// <summary>
        /// Логер событий
        /// </summary>
        protected readonly ILogger logger;
        /// <summary>
        /// Ключевое слово
        /// </summary>
        protected readonly string COLOR = "Color";

        #endregion

        #region Public Fields

        /// <summary>
        /// Параметры отрисовки объекта
        /// </summary>
        public readonly Primitive primitive;

        #endregion

        #region Ctors

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="prim">Параметры отрисовки объекта</param>
        /// <param name="log">Логер событий</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public Entity(Primitive prim, ILogger log)
        {
            primitive = prim ?? throw new System.ArgumentNullException(nameof(prim)); ;
            logger = log ?? throw new System.ArgumentNullException(nameof(log)); ;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Логика отрисовки примитива
        /// </summary>
        /// <param name="transaction">Текщуая транзакия в БД AutoCAD</param>
        /// <param name="table">Таблица блоков</param>
        /// <param name="record">Текущая запись в таблицу блоков</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        protected virtual void Draw(Transaction transaction, BlockTable table, BlockTableRecord record)
        {
            if (transaction is null)
                throw new System.ArgumentNullException(nameof(transaction));

            if (table is null)
                throw new System.ArgumentNullException(nameof(table));

            if (record is null)
                throw new System.ArgumentNullException(nameof(record));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Рисование объекта
        /// </summary>
        /// <param name="db">Текущая БД AutoCAD</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public void AppendToDrawing(Database db) 
        {
            if (db is null)
                throw new System.ArgumentNullException(nameof(db));

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
        /// <param name="prim">Рисуемый примитив</param>
        /// <param name="log">Логер событий</param>
        /// <param name="style">Стиль отрисовки</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public StyledEntity(Primitive prim, ILogger log, MyEntityStyle style) : base(prim, log)
            => this.style = style ?? throw new System.ArgumentNullException(nameof(style));

        #endregion
    }
}