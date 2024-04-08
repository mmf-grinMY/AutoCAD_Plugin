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

            var lines = DbHelper.Parse(dispatcher, primitive);

            if (lines.Length == 1)
            {
                lines[0].Append(transaction, record, primitive);
            }
            else
            {
                var block = new BlockTableRecord();
                var id = table.Add(block);
                transaction.AddNewlyCreatedDBObject(block, true);

                foreach (var line in lines)
                {
                    line.Append(transaction, block, primitive);
                }

                new BlockReference(new Point3d(0, 0, 0), id)
                {
                    Layer = primitive.LayerName
                }.AppendToDb(transaction, record, primitive);
            }
        }
    }
}