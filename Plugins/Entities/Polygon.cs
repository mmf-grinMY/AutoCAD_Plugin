using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins.Entities
{
    /// <summary>
    /// Полигон
    /// </summary>
    sealed class Polygon : Entity
    {
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="primitive">Параметры отрисовки</param>
        /// <param name="logger">Логер событий</param>
        public Polygon(Primitive primitive, Logging.ILogger logger) : base(primitive, logger) { }
        protected override void Draw(Transaction transaction, BlockTable table, BlockTableRecord record)
        {
            const string PAT_NAME = "PatName";
            const string PAT_ANGLE = "PatAngle";
            const string PAT_SCALE = "PatScale";
            const string BRUSH_COLOR = "BrushColor";

            var dictionary = SessionDispatcher.Current.LoadHatchPattern(primitive.DrawSettings);

            double GetValue(string key) => dictionary.ContainsKey(key) ? dictionary[key].ToDouble() : 1;

            var lines = Wkt.Parser.Parse(primitive.Geometry);
            var hatch = new Hatch
            {
                PatternScale = Constants.HATCH_SCALE * GetValue(PAT_SCALE),
                Color = ColorConverter.FromMMColor(primitive.DrawSettings.Value<int>(BRUSH_COLOR)),
                Layer = primitive.LayerName
            };

            hatch.AppendToDb(transaction, record, primitive);

            // FIXME: Добавить поддержку свойства ForeColor
            // На горизонте K450E нет заливок, требующих это свойство
            hatch.SetHatchPattern(HatchPatternType.PreDefined, dictionary[PAT_NAME]);

            if (dictionary.TryGetValue(PAT_ANGLE, out var angle))
            {
                hatch.PatternAngle = angle.ToDouble().ToRad();
            }

            var collection = new ObjectIdCollection();

            hatch.Associative = true;
            lines[0].SetDrawSettings(primitive.DrawSettings, primitive.LayerName).AppendToDb(transaction, record, primitive);
            collection.Add(lines[0].ObjectId);
            hatch.AppendLoop(HatchLoopTypes.Default, collection);

            for (int i = 1; i < lines.Length; i++)
            {
                collection.Clear();
                lines[i].SetDrawSettings(primitive.DrawSettings, primitive.LayerName).AppendToDb(transaction, record, primitive);
                collection.Add(lines[i].ObjectId);
                hatch.AppendLoop(HatchLoopTypes.Default, collection);
            }

            hatch.EvaluateHatch(true);
        }
    }
}