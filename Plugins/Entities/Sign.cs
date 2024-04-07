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
        readonly ITableDispatcher factory;

        #endregion

        #region Ctor

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="primitive">Параметры отрисовки</param>
        /// <param name="logger">Логер событий</param>
        /// <param name="blockDispatcher">Создатель блоков</param>
        /// <param name="style">Стиль отрисовки</param>
        public Sign(Primitive primitive, ITableDispatcher blockDispatcher, MyEntityStyle style)
            : base(primitive, style)
            => factory = blockDispatcher ?? throw new System.ArgumentNullException(nameof(blockDispatcher));

        #endregion

        #region Protected Methods

        protected override void Draw(Transaction transaction, BlockTable table, BlockTableRecord record, ILogger logger)
        {
            base.Draw(transaction, table, record, logger);

            var settings = primitive.DrawSettings;
            var key = settings.Value<string>("FontName") + "#" + settings.Value<string>("Symbol");

            if (!factory.TryAdd(key)) return;

            new BlockReference(Wkt.Parser.ParsePoint(primitive.Geometry), table[key])
            {
                Color = ColorConverter.FromMMColor(settings.Value<int>("Color")),
                Layer = primitive.LayerName,
                ScaleFactors = new Scale3d(settings.Value<string>("FontScaleX").ToDouble()) * style.scale
            }.AppendToDb(transaction, record, primitive);
        }

        #endregion
    }
}