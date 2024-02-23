using System;
using System.Windows.Forms;
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
                    //MessageBox.Show("q1");
                    polyline.SetDrawSettings(drawParams);
                    //MessageBox.Show("q2");
                    AppendToDb(polyline);
                    //MessageBox.Show("q3");
                    using (var hatch = new Hatch())
                    {
                        //MessageBox.Show("q4");

                        hatch.SetDrawSettings(drawParams);
                        //MessageBox.Show("q5");
                        AppendToDb(hatch);
                        //MessageBox.Show("q6");
                        hatch.Associative = true;
                        hatch.AppendLoop(HatchLoopTypes.Outermost, new ObjectIdCollection { polyline.ObjectId });
                        hatch.EvaluateHatch(true);
                        //MessageBox.Show("q7");
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