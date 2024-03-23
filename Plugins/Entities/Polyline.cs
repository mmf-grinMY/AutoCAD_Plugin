using Plugins.Logging;

using Autodesk.AutoCAD.DatabaseServices;

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
        /// <param name="primitive">Параметры отрисовки</param>
        public Polyline(Primitive primitive, ILogger logger) : base(primitive, logger) { }
        /// <summary>
        /// Рисование объекта
        /// </summary>
        protected override void Draw(Transaction transaction, BlockTable table, BlockTableRecord record)
        {
            foreach(var line in Wkt.Parser.Parse(primitive.Geometry))
            {
                line.SetDrawSettings(primitive.DrawSettings, primitive.LayerName).AppendToDb(transaction, record, primitive);
            }
        }
    }
}