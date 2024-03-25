using Plugins.Logging;

using System.Collections.Generic;
using System;

using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins.Dispatchers
{
    /// <summary>
    /// Диспетчер таблицы символов
    /// </summary>
    abstract class SymbolTableDispatcher
    {
        #region Private Fields

        /// <summary>
        /// Логер событий
        /// </summary>
        readonly ILogger logger;
        /// <summary>
        /// Кэш для хранения сделанных записей
        /// </summary>
        readonly HashSet<string> cache;

        #endregion

        #region Protected Fields

        /// <summary>
        /// Внeтренняя БД AutoCAD
        /// </summary>
        protected readonly Database db;

        #endregion

        #region Ctors

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="database">Внутренняя БД AutoCAD</param>
        /// <param name="log">Логер событий</param>
        public SymbolTableDispatcher(Database database, ILogger log)
        {
            db = database;
            logger = log;
            cache = new HashSet<string>();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Попытаться добавить новую запись в таблицу
        /// </summary>
        /// <typeparam name="TTable">Таблица записей</typeparam>
        /// <typeparam name="TRecord">Запись</typeparam>
        /// <param name="name">Имя новой записи</param>
        /// <param name="blockId">Id таблицы записей</param>
        /// <param name="action">Дополнительное действие при создании записи</param>
        /// <returns>true, если запись успешно добавлена или уже существует, false в противном случае</returns>
        protected bool TryAdd<TTable, TRecord>(string name, ObjectId blockId, Action<Transaction, TRecord, string> action)
            where TTable : SymbolTable
            where TRecord : SymbolTableRecord, new()
        {
            if (cache.Contains(name)) return true;

            using (var transaction = new MyTransaction(db, logger))
            {
                if (transaction.Create<TTable, TRecord>(name, blockId, action))
                {
                    cache.Add(name);
                    return true;
                }

                return false;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Попытаться добавить новую запись в таблицу
        /// </summary>
        /// <param name="name">Имя записи</param>
        /// <returns>true, если запись успешно добавлена или уже существует, false в противном случае</returns>
        public abstract bool TryAdd(string name);

        #endregion
    }
}