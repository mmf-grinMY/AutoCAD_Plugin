using System.Linq;
using System;

using Aspose.Gis.Geometries;

using APolyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

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
        /// <summary>
        /// Создание примитива
        /// </summary>
        /// <param name="line">Исходная линия</param>
        /// <returns>Примитив</returns>
        private static APolyline Create(LineString line)
        {
            var polyline = new APolyline();

            for (int i = 0; i < line.Count; i++)
            {
                var point = new Point2d(line[i].X * Constants.SCALE, line[i].Y * Constants.SCALE);
                polyline.AddVertexAt(i, point, 0, 0, 0);
            }

            return polyline;
        }
        /// <summary>
        /// Рисование объекта
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public override void Draw()
        {
            MultiLineString lines = drawParams.Geometry as MultiLineString 
                ?? throw new ArgumentException("Объект не является типом MultiLineString", nameof(drawParams.Geometry));
            foreach (LineString line in lines.Cast<LineString>())
            {
                AppendToDb(Create(line).SetDrawSettings(drawParams.DrawSettings, drawParams.LayerName));
            }
        }
    }
}