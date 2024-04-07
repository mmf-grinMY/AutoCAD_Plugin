using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins.Dispatchers
{
    /// <summary>
    /// Диспетчер таблицы слоев AutoCAD
    /// </summary>
    class LayerTableDispatcher : SymbolTableDispatcher, ITableDispatcher
    {
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="database">Внeтренняя БД AutoCAD</param>
        /// <param name="log">Логгер событий</param>
        public LayerTableDispatcher(Database database, Logging.ILogger log) : base(database, log) { }
        public bool TryAdd(string name) => TryAdd<LayerTable, LayerTableRecord>(name, db.LayerTableId, null);
    }
}
