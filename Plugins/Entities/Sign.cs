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
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="db">Внутренняя база данных AutoCAD</param>
        /// <param name="draw">Параметры отрисовки</param>
        /// <param name="box">Общий для всех рисуемых объектов BoundingBox</param>
        public Sign(Database db, DrawParams draw, Box box) : base(db, draw, box) { }
        public override void Draw()
        {
            var settings = drawParams.DrawSettings;
            var key = settings.GetProperty("FontName").GetString() + "_" + settings.GetProperty("Symbol").GetString();

            if (!HasKey(key) && !creater.Create(key))
                return;

            var point = drawParams.Geometry as Aspose.Gis.Geometries.Point
                ?? throw new ArgumentNullException($"Не удалось преобразовать объект {drawParams.Geometry} в тип {nameof(Aspose.Gis.Geometries.Point)}");

            using (var reference = new BlockReference(new Point3d(point.X * SCALE, point.Y * SCALE, 0), GetByKey(key))
            {
                Color = ColorConverter.FromMMColor(settings.GetProperty(COLOR).GetInt32()),
                Layer = drawParams.LayerName,
                ScaleFactors = new Scale3d(settings.GetProperty("FontScaleY").GetDouble())
            })
            {
                AppendToDb(reference);
            }
        }
    }
}