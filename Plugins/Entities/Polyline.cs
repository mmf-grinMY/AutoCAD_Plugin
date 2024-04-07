using Plugins.Logging;

using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace Plugins.Entities
{
    /// <summary>
    /// Полилиния
    /// </summary>
    sealed class Polyline : Entity
    {
        readonly IDbDispatcher dispatcher;

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="primitive">Параметры отрисовки</param>
        /// <param name="dispatcher">Диспетчер работы с БД</param>
        public Polyline(Primitive primitive, IDbDispatcher dispatcher) : base(primitive)
        {
            if (dispatcher is null)
                throw new System.ArgumentNullException(nameof(dispatcher));

            this.dispatcher = dispatcher;
        }
        protected override void Draw(Transaction transaction, BlockTable table, BlockTableRecord record, ILogger logger)
        {
            base.Draw(transaction, table, record, logger);

            Autodesk.AutoCAD.DatabaseServices.Polyline[] lines = Wkt.Parser.ParsePolyline(primitive.Geometry);

            if (!lines.Any())
            {
                var geometry = dispatcher.GetLongGeometry(primitive);
                lines = Wkt.Parser.ParsePolyline(geometry);

                // FIXME: ??? Необоходима ли эта проверка ???
                if (!lines.Any())
                {
                    logger.LogWarning("Для объекта {0} не смогла быть прочитана геометрия!", primitive.Guid);
                    return;
                }
            }

            if (lines.Length == 1)
            {
                lines[0]
                    .SetDrawSettings(primitive.DrawSettings, primitive.LayerName)
                    .AppendToDb(transaction, record, primitive);
            }
            else
            {
                var block = new BlockTableRecord();
                var id = table.Add(block);
                transaction.AddNewlyCreatedDBObject(block, true);

                foreach (var line in lines)
                {
                    line
                        .SetDrawSettings(primitive.DrawSettings, primitive.LayerName)
                        .AppendToDb(transaction, block, primitive);
                }

                new BlockReference(new Point3d(0, 0, 0), id)
                {
                    Layer = primitive.LayerName
                }.AppendToDb(transaction, record, primitive);
            }
        }
    }
}