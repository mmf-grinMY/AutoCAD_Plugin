#define MY_BOUNDING_BOX

using System;

using Aspose.Gis.Geometries;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using APolyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using AColor = Autodesk.AutoCAD.Colors.Color;
using System.Linq;

namespace Plugins
{
    class MMPPolyline : MMPEntity
    {
        protected const string brushColor = "BrushColor";
        protected const string brushBkColor = "BrushBkColor";
        protected const string width = "width";
        protected void ActionDrawPolyline(APolyline polyline, Transaction transaction, BlockTableRecord record, LineString line)
        {
            for (int i = 0; i < line.Count; i++)
            {
                var point = new Point2d(line[i].X * scale, line[i].Y * scale);
#if MY_BOUNDING_BOX
                CheckBoundingBox(point);
#endif
                polyline.AddVertexAt(i, point, 0, 0, 0);
            }

            // TODO: Добавить тип линий
            //polyline.Linetype = "Grantec";
            polyline.Color = ColorConverter.FromMMColor(drawParams.DrawSettings.Value<int>(brushBkColor));
            polyline.Thickness = drawParams.DrawSettings.Value<double>(width);
            polyline.Layer = drawParams.LayerName;

            record.AppendEntity(polyline);
            transaction.AddNewlyCreatedDBObject(polyline, true);
        }
        public MMPPolyline(Database db, DrawParams draw, Box box) : base(db, draw, box) { }
        public override void DrawLogic(Transaction transaction, BlockTableRecord record)
        {
            MultiLineString lines = drawParams.Geometry as MultiLineString ?? throw new ArgumentException("Объект не является типом MultiLineString", nameof(drawParams.Geometry));
            foreach (LineString line in lines.Cast<LineString>())
            {
                using (var polyline = new APolyline())
                {
                    ActionDrawPolyline(polyline, transaction, record, line);
                }
            }
        }
    }
}