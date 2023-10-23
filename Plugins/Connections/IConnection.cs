using System;
using System.Xml;

namespace Plugins
{
    //    public class CommandClass
    //    {
    //        [CommandMethod("TestCommand")]
    //        public void RunCommand()
    //        {
    //            Document doc = Application.DocumentManager.MdiActiveDocument;

    //            if (doc == null) return;

    //            Database db = doc.Database;
    //            ObjectId layerTableId = db.LayerTableId;
    //            List<string> layerNames = new List<string>();
    //            using (Transaction tr = db.TransactionManager.StartTransaction())
    //            {
    //                LayerTable layerTable = tr.GetObject(layerTableId, OpenMode.ForRead) as LayerTable;
    //                foreach (ObjectId layerTableRecordId in layerTable)
    //                {
    //                    LayerTableRecord record = tr.GetObject(layerTableRecordId, OpenMode.ForRead) as LayerTableRecord;
    //                    layerNames.Add(record.Name);
    //                }
    //                tr.Commit();
    //            }
    //            Editor editor = doc.Editor;
    //            foreach (string layerName in layerNames)
    //            {
    //                editor.WriteMessage($"\n{layerName}");
    //            }
    //        }
    //    }
    //}

    internal interface IConnection : IDisposable
    {
        XmlElement Connect();
        void Close();
    }
}