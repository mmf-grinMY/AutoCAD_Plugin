using Plugins.Dispatchers;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace Plugins.Entities
{
    /// <summary>
    /// Специальный знак
    /// </summary>
    sealed class Sign : Entity
    {
        /// <summary>
        /// Создатель блоков
        /// </summary>
        readonly SymbolTableDispatcher factory;
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="primitive">Параметры отрисовки</param>
        /// <param name="logger">Логер событий</param>
        /// <param name="creater">Создатель блоков</param>
        public Sign(Primitive primitive, Logging.ILogger logger, SymbolTableDispatcher creater) : base(primitive, logger) => factory = creater;
        protected override void Draw(Transaction transaction, BlockTable table, BlockTableRecord record)
        {
            var settings = primitive.DrawSettings;
            var key = settings.Value<string>("FontName") + "_" + settings.Value<string>("Symbol");

            if (!table.Has(key) && !factory.TryAdd(key)) return;

            new BlockReference(Wkt.Parser.ParsePoint(primitive.Geometry), table[key])
            {
                Color = ColorConverter.FromMMColor(settings.Value<int>(COLOR)),
                Layer = primitive.LayerName,
                ScaleFactors = new Scale3d(settings.Value<string>("FontScaleX").ToDouble())
            }.AppendToDb(transaction, record, primitive);
        }
    }
}