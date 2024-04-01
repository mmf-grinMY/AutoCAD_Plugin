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
        /// <param name="logger">Логер событий</param>
        public Polyline(Primitive primitive, Logging.ILogger logger) : base(primitive, logger) { }
        protected override void Draw(Transaction transaction, BlockTable table, BlockTableRecord record)
        {
            foreach(var line in Wkt.Parser.ParsePolyline(primitive.Geometry))
            {
                try
                {
                    line.SetDrawSettings(primitive.DrawSettings, primitive.LayerName).AppendToDb(transaction, record, primitive);
                }
                catch (NotDrawingLineException) { }
            }
        }
    }
}