using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins.Dispatchers
{
    class RegAppTableDispatcher : SymbolTableDispatcher
    {
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="database">Внeтренняя БД AutoCAD</param>
        /// <param name="log">Логгер событий</param>
        public RegAppTableDispatcher(Database database, Logging.ILogger log) : base(database, log) { }
        /// <summary>
        /// Попытатсья добавить новое имя приложения
        /// </summary>
        /// <param name="name">Имя нового слоя</param>
        /// <returns>true, если приложение уже существует или удачно добавлено, false в противном случае</returns>
        public override bool TryAdd(string name) => TryAdd<RegAppTable, RegAppTableRecord>(name, db.RegAppTableId, null);
    }
}
