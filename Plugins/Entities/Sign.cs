using Plugins.Dispatchers;
using Plugins.Logging;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace Plugins.Entities
{
    /// <summary>
    /// Специальный знак
    /// </summary>
    sealed class Sign : StyledEntity
    {
        #region Private Fields

        /// <summary>
        /// Создатель блоков
        /// </summary>
        readonly SymbolTableDispatcher factory;

        #endregion

        #region Ctor

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="primitive">Параметры отрисовки</param>
        /// <param name="logger">Логер событий</param>
        /// <param name="creater">Создатель блоков</param>
        /// <param name="style">Стиль отрисовки</param>
        public Sign(Primitive primitive, ILogger logger, SymbolTableDispatcher creater, MyEntityStyle style)
            : base(primitive, logger, style) 
            => factory = creater ?? throw new System.ArgumentNullException(nameof(creater));

        #endregion

        #region Protected Methods

        protected override void Draw(Transaction transaction, BlockTable table, BlockTableRecord record)
        {
            base.Draw(transaction, table, record);

            var settings = primitive.DrawSettings;
            var key = settings.Value<string>("FontName") + "_" + settings.Value<string>("Symbol");

            if (!table.Has(key) && !factory.TryAdd(key)) return;

            new BlockReference(Wkt.Parser.ParsePoint(primitive.Geometry), table[key])
            {
                Color = ColorConverter.FromMMColor(settings.Value<int>(COLOR)),
                Layer = primitive.LayerName,
                ScaleFactors = new Scale3d(settings.Value<string>("FontScaleX").ToDouble()) * style.scale
            }.AppendToDb(transaction, record, primitive);
        }

        #endregion
    }
}