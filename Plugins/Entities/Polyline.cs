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
        public Polyline(Autodesk.AutoCAD.DatabaseServices.Database db, Primitive draw, Box box) : base(db, draw, box) { }
        /// <summary>
        /// Рисование объекта
        /// </summary>
        public override void Draw()
        {
            foreach(var line in Wkt.Lines.Parse(drawParams.Geometry))
            {
                AppendToDb(line.SetDrawSettings(drawParams.DrawSettings, drawParams.LayerName));
            }
        }
    }
}