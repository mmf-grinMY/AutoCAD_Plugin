using Plugins.Logging;

using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;

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
        /// <summary>
        /// Диспетчер работы с БД
        /// </summary>
        readonly OracleDbDispatcher dispatcher;

        #endregion

        #region Ctor

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="primitive">Параметры отрисовки</param>
        /// <param name="logger">Логер событий</param>
        /// <param name="loader">Загрузчик штриховок</param>
        /// <param name="style">Стиль отрисовки</param>
        /// <param name="dispatcher">Диспетчер работы с БД</param>
        public Polygon(Primitive primitive,
                       ILogger logger,
                       HatchPatternLoader loader,
                       MyHatchStyle style,
                       OracleDbDispatcher dispatcher)
            : base(primitive, logger, style)
        {
            this.loader = loader ?? throw new System.ArgumentNullException(nameof(loader));
            this.dispatcher = dispatcher ?? throw new System.ArgumentNullException(nameof(dispatcher));
        }

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
            {
                var geometry = dispatcher.GetLongGeometry(primitive);
                lines = Wkt.Parser.ParsePolyline(geometry);

                // Объект с ObjectGuid 'C173414D-AE5E-43AF-9110-0D9EF20553D8' хранит не полную геометрию,
                // но в таблице допгеометрии нет записей для него
                if (!lines.Any())
                {
                    logger.LogWarning("Для объекта {0} не смогла быть прочитана геометрия!", primitive.Guid);
                    return;
                }
            }

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

            try
            {
                // FIXME: Добавить поддержку свойства ForeColor
                // На горизонте K450E нет заливок, требующих это свойство
                hatch.SetHatchPattern(HatchPatternType.PreDefined, dictionary[PAT_NAME]);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                if (e.Message.Contains("eInvalidInput"))
                    logger.LogError("Не удалось найти штриховку " + dictionary[PAT_NAME] + "!");
                else
                    throw;
            }

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