using System;
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
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="primitive">Параметры отрисовки</param>
        /// <param name="logger">Логер событий</param>
        public Polyline(Primitive primitive, Logging.ILogger logger) : base(primitive, logger) { }
        protected override void Draw(Transaction transaction, BlockTable table, BlockTableRecord record)
        {
            base.Draw(transaction, table, record);

            Autodesk.AutoCAD.DatabaseServices.Polyline[] polylines = null;

            try
            {
                polylines = Wkt.Parser.ParsePolyline(primitive.Geometry);

                if (polylines is null || !polylines.Any())
                    throw new ArgumentException(primitive.Geometry);
            }
            catch (ArgumentException)
            {

            }

            if (polylines.Length == 1)
            {
                polylines[0]
                    .SetDrawSettings(primitive.DrawSettings, primitive.LayerName)
                    .AppendToDb(transaction, record, primitive);
            }
            else
            {
                var block = new BlockTableRecord();
                var id = table.Add(block);
                transaction.AddNewlyCreatedDBObject(block, true);

                foreach (var line in polylines)
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