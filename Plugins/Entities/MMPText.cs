using System;
using Newtonsoft.Json.Linq;
using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins
{
    class MMPText : MMPBaseText
    {
        public MMPText(Database db, DrawParams draw, Box box) : base(db, draw, box) { }
        public override void DrawLogic(Transaction transaction, BlockTableRecord record)
        {
            int fontSize = Convert.ToInt32(drawParams.DrawSettings["FontSize"].Value<string>());
            string textString = drawParams.DrawSettings["Text"].Value<string>();
            CreateText(transaction, record, false, fontSize, textString);
        }
    }
}