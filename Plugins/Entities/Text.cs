using System;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Newtonsoft.Json.Linq;

using static Plugins.Constants;

namespace Plugins.Entities
{
    /// <summary>
    /// Подпись
    /// </summary>
    sealed class Text : Entity
    {
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="db">Внутренняя база данных AutoCAD</param>
        /// <param name="draw">Параметры отрисовки</param>
        /// <param name="box">Общий для всех рисуемых объектов BoundingBox</param>
        public Text(Database db, DrawParams draw, Box box) : base(db, draw, box) { }
        /// <summary>
        /// Рисование примитива
        /// </summary>
        public override void Draw()
        {
            const string FONT_SIZE = "FontSize";
            const string ANGLE = "Angle";
            const string TEXT = "Text";

            var settings = drawParams.DrawSettings;
            var fontSize = settings.Value<int>(FONT_SIZE) * TEXT_SCALE;

            var point = drawParams.Geometry as Aspose.Gis.Geometries.Point;
            
            using (var text = new DBText()
            {
                Layer = drawParams.LayerName,
                Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 0, 0),
                Position = new Point3d(point.X * SCALE, point.Y * SCALE, 0),
            })
            {
                if (fontSize > 0)
                    text.Height = fontSize;

                AppendToDb(text);

                // TODO: Проверить параметры установки поворота текста
                if (settings.TryGetValue(ANGLE, StringComparison.CurrentCulture, out JToken angle))
                    text.Rotation = angle.Value<string>().ToDouble().ToRad();

                text.TextString = settings.Value<string>(TEXT);
            }
        }
    }
}