using System;
using System.Linq;

using Aspose.Gis.Geometries;

using APolyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using static Plugins.Constants;

namespace Plugins.Entities
{  
    /// <summary>
    /// Полилиния
    /// </summary>
    class Polyline : Entity
    {
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="db">Внутренняя база данных AutoCAD</param>
        /// <param name="draw">Параметры отрисовки</param>
        /// <param name="box">Общий для всех рисуемых объектов BoundingBox</param>
        public Polyline(Database db, DrawParams draw, Box box) : base(db, draw, box) { }
        public override void Draw()
        {
            MultiLineString lines = drawParams.Geometry as MultiLineString 
                ?? throw new ArgumentException("Объект не является типом MultiLineString", nameof(drawParams.Geometry));
            foreach (LineString line in lines.Cast<LineString>())
            {
                using (var polyline = EntityCreater.Create(line))
                {
                    polyline.SetDrawSettings(drawParams);

                    AppendToDb(polyline);
                }
            }
        }
    }
    static class EntityCreater
    {
        public static APolyline Create(LineString line)
        {
            var polyline = new APolyline();

            for (int i = 0; i < line.Count; i++)
            {
                var point = new Point2d(line[i].X * SCALE, line[i].Y * SCALE);
                polyline.AddVertexAt(i, point, 0, 0, 0);
            }

            return polyline;
        }
        public static APolyline Create(Aspose.Gis.Geometries.Polygon polygon)
        {
            var polyline = new APolyline();

            int i = 0;
            foreach (var p in polygon.ExteriorRing)
            {
                var point = new Point2d(p.X * SCALE, p.Y * SCALE);
                polyline.AddVertexAt(i, point, 0, 0, 0);
                ++i;
            }

            return polyline;
        }
    }
}