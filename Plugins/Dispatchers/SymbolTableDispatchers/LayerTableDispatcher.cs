using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins.Dispatchers
{
    /// <summary>
    /// Диспетчер таблицы слоев AutoCAD
    /// </summary>
    class LayerTableDispatcher : SymbolTableDispatcher
    {
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="database">Внeтренняя БД AutoCAD</param>
        /// <param name="log">Логгер событий</param>
        public LayerTableDispatcher(Database database, Logging.ILogger log) : base(database, log) { }
        /// <summary>
        /// Попытатсья добавить новый слой
        /// </summary>
        /// <param name="name">Имя нового слоя</param>
        /// <returns>true, если слой уже существует или удачно добавлен, false в противном случае</returns>
        public override bool TryAdd(string name) => TryAdd<LayerTable, LayerTableRecord>(name, db.LayerTableId, null);
    }
}
