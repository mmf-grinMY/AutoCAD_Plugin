#define MY_BOUNDING_BOX

using System;
using Newtonsoft.Json.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace Plugins
{
    abstract class MMPBaseText : MMPEntity
    {
        protected readonly int textScale = 750;
        public MMPBaseText(Database db, DrawParams draw, Box box) : base(db, draw, box) { }
        protected void CreateText(Transaction transaction, BlockTableRecord record, bool isSign, int fontSize, string textString)
        {
            var point = drawParams.Geometry as Aspose.Gis.Geometries.Point
                ?? throw new ArgumentNullException($"Не удалось преобразовать объект {drawParams.Geometry} в тип {nameof(Aspose.Gis.Geometries.Point)}");
            Point3d position = new Point3d(point.X * scale, point.Y * scale, 0);
            using (var text = new DBText())
            {
                var textStyleTable = transaction.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
                text.SetDatabaseDefaults();
                if (isSign)
                {
                    text.TextStyleId = textStyleTable[drawParams.DrawSettings["FontName"].Value<string>()];
                    text.Color = ColorConverter.FromMMColor(drawParams.DrawSettings.Value<int>("Color"));
                }
                text.Position = position;
                if (fontSize > 0)
                    text.Height = fontSize;
                text.TextString = textString;
                text.Layer = drawParams.LayerName;

#if MY_BOUNDING_BOX
                CheckBoundingBox(position);
#endif

                record.AppendEntity(text);
                transaction.AddNewlyCreatedDBObject(text, true);
            }
        }
    }
}