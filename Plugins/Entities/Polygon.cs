using System.Linq;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;

namespace Plugins.Entities
{
    /// <summary>
    /// Полигон
    /// </summary>
    sealed class Polygon : StyledEntity
    {
        #region Private Fields

        /// <summary>
        /// Загрузчик штриховок
        /// </summary>
        readonly HatchPatternLoader loader;
        readonly OracleDbDispatcher dispatcher;
        readonly string gorizont;

        #endregion

        #region Ctor

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="primitive">Параметры отрисовки</param>
        /// <param name="logger">Логер событий</param>
        /// <param name="loader">Загрузчик штриховок</param>
        /// <param name="style">Стиль отрисовки</param>
        public Polygon(Primitive primitive, Logging.ILogger logger, HatchPatternLoader loader, MyHatchStyle style) 
            : base(primitive, logger, style) 
            => this.loader = loader ?? throw new System.ArgumentNullException(nameof(loader));

        #endregion

        #region Protected Methods
        
        // TODO: Раздробить метод
        protected override void Draw(Transaction transaction, BlockTable table, BlockTableRecord record)
        {
            base.Draw(transaction, table, record);

            const string PAT_NAME = "PatName";
            const string PAT_ANGLE = "PatAngle";
            const string PAT_SCALE = "PatScale";
            const string BRUSH_COLOR = "BrushColor";

            var dictionary = loader.Load(primitive.DrawSettings);

            double GetValue(string key)
            {
                if (dictionary is null)
                    return 1.0;

                return dictionary.ContainsKey(key) ? dictionary[key].ToDouble() : 1.0;
            }

            var lines = Wkt.Parser.ParsePolyline(primitive.Geometry);

            if (!lines.Any())
#if OLD
                return;
#else
                lines = Wkt.Parser.ParsePolyline();
#endif

            if (lines[0].Area == 0)
                return;

            if (dictionary is null)
            {
                foreach (var line in lines)
                {
                    line.SetDrawSettings(primitive.DrawSettings, primitive.LayerName).AppendToDb(transaction, record, primitive);
                }

                return;
            }

            // TODO: Вынести прозрачность заливки как конфигурационный параметр
            var hatch = new Hatch
            {
                PatternScale = style.scale * GetValue(PAT_SCALE),
                Transparency = new Autodesk.AutoCAD.Colors.Transparency((style as MyHatchStyle).transparency),
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

#endregion
    }
}