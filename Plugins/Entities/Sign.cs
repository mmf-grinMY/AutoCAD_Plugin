using System;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using static Plugins.Constants;

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
        private readonly static BlocksCreater creater;
        static Sign()
        {
            creater = new BlocksCreater();
        }
        const string FONT_SCALE_Y = "FontScaleY";
        const string SYMBOL = "Symbol";
        const string FONT_NAME = "FontName";
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="db">Внутренняя база данных AutoCAD</param>
        /// <param name="draw">Параметры отрисовки</param>
        /// <param name="box">Общий для всех рисуемых объектов BoundingBox</param>
        /// <param name="counter">Счетчик ошибок</param>
        public Sign(Database db, DrawParams draw, Box box
#if DEBUG
            , ErrorCounter counter
#endif
            ) : base(db, draw, box
#if DEBUG
                , counter
#endif
                ) { }
        protected override void DrawLogic(Transaction transaction, BlockTableRecord record)
        {
            string size = drawParams.DrawSettings.Value<string>(FONT_SCALE_Y);
            int fontSize = Convert.ToInt32(Convert.ToDouble(size));

            string key = drawParams.DrawSettings.Value<string>(FONT_NAME) + "_" + drawParams.DrawSettings.Value<string>(SYMBOL);
            var table = transaction.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;

            if (!table.Has(key) && !creater.Create(key))
                return;

            const string COLOR = "Color";

            var point = drawParams.Geometry as Aspose.Gis.Geometries.Point
                ?? throw new ArgumentNullException($"Не удалось преобразовать объект {drawParams.Geometry} в тип {nameof(Aspose.Gis.Geometries.Point)}");
            Point3d position = new Point3d(point.X * Scale, point.Y * Scale, 0);
            var reference = new BlockReference(position, table[key])
            {
                Color = ColorConverter.FromMMColor(drawParams.DrawSettings.Value<int>(COLOR)),
                Layer = drawParams.LayerName
            };

            CheckBounds(reference);
            record.AppendEntity(reference);
            transaction.AddNewlyCreatedDBObject(reference, true);
        }
    }
}