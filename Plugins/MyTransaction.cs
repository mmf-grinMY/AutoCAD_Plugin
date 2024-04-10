using Plugins.Logging;

using System;

using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins
{
    /// <summary>
    /// Безопасная относительно исключений обертка для Autodesk.AutoCAD.DatabaseServices.Transaction
    /// </summary>
    class MyTransaction : IDisposable
    {
        #region Private Fields

        /// <summary>
        /// Логер событий
        /// </summary>
        readonly ILogger logger;
        /// <summary>
        /// База данных текущего документа
        /// </summary>
        readonly Database db;
        /// <summary>
        /// Открытая транзакция
        /// </summary>
        readonly Transaction transaction;

        #endregion

        #region Ctor

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="database">База данных текщуего документа AutoCAD</param>
        /// <param name="log">лоегр событий</param>
        /// <exception cref="ArgumentNullException"></exception>
        public MyTransaction(Database database, ILogger log)
        {
            db = database ?? throw new ArgumentNullException(nameof(database));
            logger = log ?? throw new ArgumentNullException(nameof(log));
            transaction = db.TransactionManager.StartTransaction();
        }

        #endregion

        #region

        /// <summary>
        /// Создание записи в таблицу символов
        /// </summary>
        /// <typeparam name="TTable">Таблица символов</typeparam>
        /// <typeparam name="TRecord">Запись в таблицу символов</typeparam>
        /// <param name="name">Имя записи</param>
        /// <param name="blockId">Id таблицы символов</param>
        /// <param name="action">Дополнительное действие для записи</param>
        /// <returns>trueб если запись успешно добавлена или уже существует, false в противном случае</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool Create<TTable, TRecord>(string name, ObjectId blockId, Action<Transaction, TRecord, string> action = null)
            where TTable : SymbolTable
            where TRecord : SymbolTableRecord, new()
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            try
            {
                var table = transaction.GetObject(blockId, OpenMode.ForRead) as TTable;
                TRecord record = null;

                if (!table.Has(name))
                {
                    table.UpgradeOpen();
                    record = new TRecord() { Name = name };

                    table.Add(record);
                    transaction.AddNewlyCreatedDBObject(record, true);
                }
                else
                {
                    record =  transaction.GetObject(table[name], OpenMode.ForRead) as TRecord;
                }

                action?.Invoke(transaction, record, name);
            }
            // Не найден файл описания знака
            catch (System.IO.FileNotFoundException) { throw; }
            catch (Exception e)
            {
                logger.LogError(e);
                return false;
            }

            return true;
        }
        /// <summary>
        /// Добавление объекта в существующую запись блоков
        /// </summary>
        /// <param name="action">Действие записи</param>
        public void AddBlockRecord(Action<Transaction, BlockTable, BlockTableRecord, ILogger> action)
        {
            try
            {
                var table = transaction.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                var record = transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                action(transaction, table, record, logger);
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
        }
        /// <summary>
        /// Освоождение неуправляемых ресурсов
        /// </summary>
        public void Dispose()
        {
            if (transaction != null)
            {
                transaction.Commit();
                transaction.Dispose();
            }
        }

        #endregion
    }
}
