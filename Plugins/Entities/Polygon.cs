using System;

using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins.Entities
{
    /// <summary>
    /// Полигон
    /// </summary>
    sealed class Polygon : Entity
    {
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="db">Внутренняя база данных AutoCAD</param>
        /// <param name="draw">Параметры отрисовки</param>
        /// <param name="box">Общий для всех рисуемых объектов BoundingBox</param>
        public Polygon(Database db, DrawParams draw, Box box) : base(db, draw, box) { }
        public override void Draw()
        {
            using (var polyline = EntityCreater.Create(drawParams.Geometry as Aspose.Gis.Geometries.Polygon))
            {
                try
                {
                    polyline.SetDrawSettings(drawParams);

                    using (var hatch = new Hatch())
                    {
                        hatch.SetDrawSettings(drawParams);

                        AppendToDb(hatch);

                        hatch.Associative = true;
                        hatch.AppendLoop(HatchLoopTypes.Outermost, new ObjectIdCollection { polyline.ObjectId });
                        hatch.EvaluateHatch(true);
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message != "eInvalidInput")
                    {
                        throw ex;
                    }
                }
            }
        }
    }
}