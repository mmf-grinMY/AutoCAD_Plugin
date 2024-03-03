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
        /// <summary>
        /// Статическое создание
        /// </summary>
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
        public Sign(Database db, Primitive draw, Box box) : base(db, draw, box) { }
        /// <summary>
        /// Рисование объекта
        /// </summary>
        public override void Draw()
        {
            var settings = drawParams.DrawSettings;
            var key = settings.Value<string>("FontName") + "_" + settings.Value<string>("Symbol");
            if (!HasKey(key) && !creater.Create(key))
                return;

            using (var reference = new BlockReference(Wkt.Lines.ParsePoint(drawParams.Geometry), GetByKey(key))
            {
                Color = ColorConverter.FromMMColor(settings.Value<int>(COLOR)),
                Layer = drawParams.LayerName,
                ScaleFactors = new Scale3d(settings.Value<string>("FontScaleX").ToDouble())
            })
            {
                AppendToDb(reference);
            }
        }
    }
}