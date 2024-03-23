using Plugins.Logging;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace Plugins.Entities
{
    /// <summary>
    /// Специальный знак
    /// </summary>
    sealed class Sign : Entity
    {
        readonly IBlocksCreater factory;

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="primitive">Параметры отрисовки</param>
        public Sign(Primitive primitive, ILogger logger, IBlocksCreater creater) : base(primitive, logger) => factory = creater;
        /// <summary>
        /// Рисование объекта
        /// </summary>
        protected override void Draw(Transaction transaction, BlockTable table, BlockTableRecord record)
        {
            try
            {
                var settings = primitive.DrawSettings;
                var key = settings.Value<string>("FontName") + "_" + settings.Value<string>("Symbol");

                if (!table.Has(key) && !factory.Create(key)) return;

                new BlockReference(Wkt.Parser.ParsePoint(primitive.Geometry), table[key])
                {
                    Color = ColorConverter.FromMMColor(settings.Value<int>(COLOR)),
                    Layer = primitive.LayerName,
                    ScaleFactors = new Scale3d(settings.Value<string>("FontScaleX").ToDouble())
                }.AppendToDb(transaction, record, primitive);
            }
            catch (System.Exception ex)
            {
                logger.Log(LogLevel.Error, exception: ex);
            }
        }
    }
}