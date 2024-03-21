using System;

using Autodesk.AutoCAD.DatabaseServices;

using Newtonsoft.Json.Linq;

namespace Plugins.Entities
{
    // TODO: Учитывать смещение текста
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
        public Text(Database db, Primitive draw) : base(db, draw) { }
        /// <summary>
        /// Рисование примитива
        /// </summary>
        public override void Draw()
        {
            const string FONT_SIZE = "FontSize";
            const string ANGLE = "Angle";
            const string TEXT = "Text";

            var settings = primitive.DrawSettings;
            var fontSize = settings.Value<int>(FONT_SIZE) * Constants.TEXT_SCALE;

            using (var text = new DBText()
            {
                Layer = primitive.LayerName,
                Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 0, 0),
                Position = Wkt.Lines.ParsePoint(primitive.Geometry),
            })
            {
                if (fontSize > 0)
                    text.Height = fontSize;

                AppendToDb(text);

                if (primitive.Param.TryGetValue(ANGLE, StringComparison.CurrentCulture, out JToken angle))
                {
                    text.Rotation = angle.Value<string>().Replace('_', '.').ToDouble().ToRad();
                }

                text.TextString = settings.Value<string>(TEXT);
            }
        }
    }
}