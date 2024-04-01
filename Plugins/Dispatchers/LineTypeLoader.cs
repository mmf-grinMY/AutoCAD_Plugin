//using Autodesk.AutoCAD.DatabaseServices;
//using Autodesk.AutoCAD.Internal;

//namespace Plugins.Dispatchers
//{
//    class LineTypeLoader
//    {
//        public void Test()
//        {
//            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
//            var db = doc.Database;
//            using (var transaction = db.TransactionManager.StartTransaction())
//            {
//                var table = transaction.GetObject(db.LinetypeTableId, Autodesk.AutoCAD.DatabaseServices.OpenMode.ForWrite) as LinetypeTable;
//                var record = new LinetypeTableRecord();

//                transaction.Commit();
//            }
//        }
//    }
//}

using System;

using cad = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

// [assembly: CommandClass(typeof(Bushman.CAD.Samples.Styles.MultilineStyleSample))]

namespace Bushman.CAD.Samples.Styles
{
    public class MultilineStyleSample
    {
        [CommandMethod("CreateMultilineStyle1", CommandFlags.Modal)]
        public void CreateMLineStyle()
        {
            var doc = cad.DocumentManager.MdiActiveDocument;
            if (doc is null) return;
            var db = doc.Database;

            using (var transaction = db.TransactionManager.StartTransaction())
            {
                var mlDict = transaction.GetObject(db.MLStyleDictionaryId, OpenMode.ForWrite) as DBDictionary;
                var mlineStyleName = "Пример";

                if (!mlDict.Contains(mlineStyleName))
                {
                    var mlineStyle = new MlineStyle
                    {
                        Name = mlineStyleName,
                        Description = "Некоторое описание"
                    };

                    mlDict.SetAt(mlineStyleName, mlineStyle);
                    transaction.AddNewlyCreatedDBObject(mlineStyle, true);

                    var angleGrad = 90.0;
                    var angleRadian = angleGrad * Math.PI / 180;

                    // Start line
                    mlineStyle.StartSquareCap = true;
                    // Start Outer arcs
                    mlineStyle.StartRoundCap = true;
                    // Start Inner arcs
                    mlineStyle.StartInnerArcs = true;
                    // Start angle
                    mlineStyle.StartAngle = angleRadian;

                    // End line
                    mlineStyle.EndSquareCap = true;
                    // End Outer arcs
                    mlineStyle.EndRoundCap = true;
                    // End Inner arcs
                    mlineStyle.EndInnerArcs = true;
                    // End angle
                    mlineStyle.EndAngle = angleRadian;

                    var color = Color.FromRgb(255, 0, 0);

                    mlineStyle.Filled = true;
                    mlineStyle.FillColor = color;
                    mlineStyle.ShowMiters = true;

                    var element = new MlineStyleElement(0.15, color, db.Celtype);
                    mlineStyle.Elements.Add(element, true);

                    element = new MlineStyleElement(-0.15, color, db.Celtype);
                    mlineStyle.Elements.Add(element, false);
                }
                transaction.Commit();
            }
        }
        [CommandMethod("createmlinestyle2")]
        public void CreateMLineStyle2()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            using (var transaction = db.TransactionManager.StartTransaction())
            {
                var mlineDic = transaction.GetObject(db.MLStyleDictionaryId, OpenMode.ForRead) as DBDictionary;
                if (!mlineDic.Contains("TEST"))
                {
                    mlineDic.UpgradeOpen();
                    
                    var color = Color.FromRgb(255, 0, 0);
                    var mlineStyle = new MlineStyle();
                    
                    mlineDic.SetAt("TEST", mlineStyle);
                    transaction.AddNewlyCreatedDBObject(mlineStyle, true);

                    mlineStyle.EndAngle = Math.PI * 0.5;
                    mlineStyle.StartAngle = Math.PI * 0.5;
                    mlineStyle.Name = "TEST";
                    mlineStyle.Elements.Add(new MlineStyleElement(0.25, color, db.Celtype), true);
                    mlineStyle.Elements.Add(new MlineStyleElement(-0.25, color, db.Celtype), false);
                }

                transaction.Commit();
            }
        }
    }
}