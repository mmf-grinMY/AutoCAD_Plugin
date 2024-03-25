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
        /// <param name="primitive">Параметры отрисовки</param>
        /// <param name="logger">Логер событий</param>
        public Text(Primitive primitive, Logging.ILogger logger) : base(primitive, logger) { }
        protected override void Draw(Transaction transaction, BlockTable table, BlockTableRecord record)
        {
            const string FONT_SIZE = "FontSize";
            const string ANGLE = "Angle";
            const string TEXT = "Text";

            var settings = primitive.DrawSettings;
            var fontSize = settings.Value<int>(FONT_SIZE) * Constants.TEXT_SCALE;

            var text = new DBText()
            {
                Layer = primitive.LayerName,
                Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 0, 0),
                Position = Wkt.Parser.ParsePoint(primitive.Geometry),
                TextString = settings.Value<string>(TEXT),
                Height = fontSize
            };

            if (primitive.Param.TryGetValue(ANGLE, System.StringComparison.CurrentCulture, out JToken angle))
            {
                text.Rotation = angle.Value<string>().Replace('_', '.').ToDouble().ToRad();
            }

            text.AppendToDb(transaction, record, primitive);
        }
    }
}