using Plugins.Logging;

using System;

using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins
{
    class MyTransaction : IDisposable
    {
        readonly ILogger logger;
        readonly Database db;
        readonly Transaction transaction;
        public MyTransaction(Database database, ILogger log)
        {
            db = database;
            logger = log;
            transaction = db.TransactionManager.StartTransaction();
        }
        public bool Create<TTable, TRecord>(string name, ObjectId blockId, Action<Transaction, TRecord, string> action = null)
            where TTable : SymbolTable
            where TRecord : SymbolTableRecord, new()
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            try
            {
                var table = transaction.GetObject(blockId, OpenMode.ForWrite) as TTable;
                var record = new TRecord() { Name = name };

                table.Add(record);
                transaction.AddNewlyCreatedDBObject(record, true);
                action?.Invoke(transaction, record, name);
            }
            catch (Exception e)
            {
                if (e.StackTrace.Contains("в Autodesk.AutoCAD.DatabaseServices.SymbolTableRecord.set_Name(String name)"))
                {
                    logger.LogInformation(name);
                }
                else if (e.Data.Count != 0)
                {
                    logger.LogInformation("Не определен символ \"" + e.Data["Symbol"] + "\" из шрифта \"" + e.Data["FontName"] + "\"!");
                }
                else
                {
                    logger.LogError(e);
                }

                return false;
            }

            return true;
        }
        public void AddBlockRecord(Action<Transaction, BlockTable, BlockTableRecord> action)
        {
            try
            {
                var table = transaction.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                var record = transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                action(transaction, table, record);
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
        }
        public void Dispose()
        {
            if (transaction != null)
            {
                transaction.Commit();
                transaction.Dispose();
            }
        }
    }
}
