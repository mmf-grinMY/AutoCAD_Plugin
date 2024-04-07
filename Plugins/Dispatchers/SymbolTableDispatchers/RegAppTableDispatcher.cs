using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins.Dispatchers
{
    class RegAppTableDispatcher : SymbolTableDispatcher, ITableDispatcher
    {
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="database">Внeтренняя БД AutoCAD</param>
        /// <param name="log">Логгер событий</param>
        public RegAppTableDispatcher(Database database, Logging.ILogger log) : base(database, log) { }
        public bool TryAdd(string name) => TryAdd<RegAppTable, RegAppTableRecord>(name, db.RegAppTableId, null);
    }
}
