using Plugins.Logging;

using System.IO;

using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins.Dispatchers
{
    /// <summary>
    /// Диспетчер таблицы блоков
    /// </summary>
    class BlockTableDispatcher : SymbolTableDispatcher, ITableDispatcher
    {
        #region Ctors

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="database">Внутренняя БД AutoCAD</param>
        /// <param name="log">Текущий логер событий</param>
        public BlockTableDispatcher(Database database, ILogger log) : base(database, log) { }

        #endregion

        #region Private Methods

        /// <summary>
        /// Заполнение записи блока из файла
        /// </summary>
        /// <param name="transaction">Текущая открытая транзакция</param>
        /// <param name="record">Запись в таблицу блоков</param>
        /// <param name="name">Имя новой записи блока</param>
        /// <exception cref="FileNotFoundException"></exception>
        void Create(Transaction transaction, BlockTableRecord record, string name)
        {
            using (var source = new Database())
            {
                var path = Path.Combine(Constants.AssemblyPath, "Blocks", name + ".dwg");

                try
                {
                    source.ReadDwgFile(path, FileOpenMode.OpenForReadAndWriteNoShare, false, string.Empty);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception)
                {
                    throw new FileNotFoundException(path);
                }

                var oids = GetDbModelEntities(source);

                if (oids.Count > 0)
                {
                    var mapping = new IdMapping();
                    source.WblockCloneObjects(oids, record.Id, mapping, DuplicateRecordCloning.Ignore, false);
                }
            }
        }
        /// <summary>
        /// Копирование коллекции всех объектов из БД
        /// </summary>
        /// <param name="db">БД копируемого документа</param>
        /// <returns>Коллекция Id объектов</returns>
        ObjectIdCollection GetDbModelEntities(Database db)
        {
            var collection = new ObjectIdCollection();

            using (var transaction = db.TransactionManager.StartTransaction())
            {
                var record = transaction.GetObject(db.CurrentSpaceId, OpenMode.ForRead) as BlockTableRecord;

                foreach (var id in record)
                {
                    if (transaction.GetObject(id, OpenMode.ForRead) is Entity)
                    {
                        collection.Add(id);
                    }
                }
            }

            return collection;
        }

        #endregion

        #region Public Methods

        public bool TryAdd(string name)
        {
            try
            {
                return TryAdd<BlockTable, BlockTableRecord>(name, db.BlockTableId, Create);
            }
            catch (FileNotFoundException)
            {
                cache.Add(name);
                logger.LogInformation("Не найден файл определения блока \"{0}\"!", name);
                return false;
            }
        }

        #endregion
    }
}
