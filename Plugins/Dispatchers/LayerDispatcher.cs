using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins
{
    class LayerDispatcher
    {
        readonly HashSet<string> cache;
        readonly Database db;
        public LayerDispatcher()
        {
            db = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database;
            cache = new HashSet<string>();
        }
        public bool TryAdd(string layerName)
        {
            if (cache.Contains(layerName)) return true;

            cache.Add(layerName);

            using (var transaction = db.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(db.LayerTableId, OpenMode.ForWrite) as LayerTable;
                var record = new LayerTableRecord { Name = layerName };

                table.Add(record);
                transaction.AddNewlyCreatedDBObject(record, true);
                transaction.Commit();
            }

            return true;
        }
    }
}