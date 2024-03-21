namespace Plugins.Entities
{
    /// <summary>
    /// Полилиния
    /// </summary>
    sealed class Polyline : Entity
    {
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="db">Внутренняя база данных AutoCAD</param>
        /// <param name="draw">Параметры отрисовки</param>
        public Polyline(Autodesk.AutoCAD.DatabaseServices.Database db, Primitive draw) : base(db, draw) { }
        /// <summary>
        /// Рисование объекта
        /// </summary>
        public override void Draw()
        {
            foreach(var line in Wkt.Lines.Parse(primitive.Geometry))
            {
                AppendToDb(line.SetDrawSettings(primitive.DrawSettings, primitive.LayerName));
            }
        }
    }
}