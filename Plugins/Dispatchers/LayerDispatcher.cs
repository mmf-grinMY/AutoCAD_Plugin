using Plugins.Logging;

using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins
{
    /// <summary>
    /// Диспетчер слоев AutoCAD
    /// </summary>
    class LayerDispatcher
    {
        readonly ILogger logger;
        /// <summary>
        /// Кэш для отрисованных слоев
        /// </summary>
        readonly HashSet<string> cache;
        /// <summary>
        /// Внeтренняя БД AutoCAD
        /// </summary>
        readonly Database db;
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="database">Внeтренняя БД AutoCAD</param>
        /// <param name="log">Логгер событий</param>
        public LayerDispatcher(Database database, ILogger log)
        {
            db = database;
            cache = new HashSet<string>();
        }
        /// <summary>
        /// Попытатсья добавить новый слой
        /// </summary>
        /// <param name="layerName">Имя нового слоя</param>
        /// <returns>true, если слой уже существует или удачно добавлен, false в противном случае</returns>
        public bool TryAdd(string layerName)
        {
            if (cache.Contains(layerName)) return true;

            cache.Add(layerName);

            Transaction transaction = null;

            try 
            {
                transaction = db.TransactionManager.StartTransaction();
                var table = transaction.GetObject(db.LayerTableId, OpenMode.ForWrite) as LayerTable;
                var record = new LayerTableRecord { Name = layerName };

                table.Add(record);
                transaction.AddNewlyCreatedDBObject(record, true);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                logger.LogError(e);
            }
            finally
            {
                if (transaction != null)
                {
                    transaction.Commit();
                    transaction.Dispose();
                }
            }

            return true;
        }
    }
}