using System;
using Newtonsoft.Json.Linq;
using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins
{
    class MMPSign : MMPBaseText
    {
        const string fontScaleY = "FontScaleY";
        const string symbol = "Symbol";
        public MMPSign(Database db, DrawParams draw, Box box) : base(db, draw, box) { }
        public override void DrawLogic(Transaction transaction, BlockTableRecord record)
        {
            string size = drawParams.DrawSettings.Value<string>(fontScaleY);
            int fontSize = (int)(Convert.ToDouble(size) * textScale);
            char chr = Convert.ToChar(drawParams.DrawSettings.Value<int>(symbol) + 1);
            CreateText(transaction, record, true, fontSize, chr.ToString());
        }
    }
}