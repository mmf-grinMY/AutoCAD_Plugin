using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Xml.Linq;

using APolyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Newtonsoft.Json.Linq;

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
        /// <param name="db">Внутренняя база данных AutoCAD</param>
        /// <param name="draw">Параметры отрисовки</param>
        public Polygon(Database db, Primitive draw) : base(db, draw) { }
        /// <summary>
        /// Рисование объекта
        /// </summary>
        public override void Draw()
        {
            const string PAT_NAME = "PatName";
            const string PAT_ANGLE = "PatAngle";
            const string PAT_SCALE = "PatScale";
            const string brushColor = "BrushColor";

            var dictionary = HatchPatternLoader.Load(primitive.DrawSettings);

            double GetValue(string key) => dictionary.ContainsKey(key) ? dictionary[key].ToDouble() : 1;

            var lines = Wkt.Lines.Parse(primitive.Geometry);
            var hatch = new Hatch
            {
                PatternScale = Constants.HATCH_SCALE * GetValue(PAT_SCALE),
                Color = ColorConverter.FromMMColor(primitive.DrawSettings.Value<int>(brushColor)),
                Layer = primitive.LayerName
            };

            AppendToDb(hatch);

            // FIXME: Добавить поддержку свойства ForeColor
            // На горизонте K450E нет заливок, требующих это свойство
            hatch.SetHatchPattern(HatchPatternType.PreDefined, dictionary[PAT_NAME]);

            if (dictionary.TryGetValue(PAT_ANGLE, out var angle))
            {
                hatch.PatternAngle = angle.ToDouble().ToRad();
            }

            var collection = new ObjectIdCollection();

            hatch.Associative = true;
            AppendToDb(lines[0].SetDrawSettings(primitive.DrawSettings, primitive.LayerName));
            collection.Add(lines[0].ObjectId);
            hatch.AppendLoop(HatchLoopTypes.Default, collection);

            for (int i = 1; i < lines.Length; i++)
            {
                collection.Clear();
                AppendToDb(lines[i].SetDrawSettings(primitive.DrawSettings, primitive.LayerName));
                collection.Add(lines[i].ObjectId);
                hatch.AppendLoop(HatchLoopTypes.Default, collection);
            }

            hatch.EvaluateHatch(true);
        }
        /// <summary>
        /// Загрузчик параметров паттернов штриховки
        /// </summary>
        static class HatchPatternLoader
        {
            /// <summary>
            /// Кэш параметров заливок
            /// </summary>
            private static readonly Dictionary<string, Dictionary<string, string>> cache;
            /// <summary>
            /// Корневой узел файла конфигурации паттернов штриховки
            /// </summary>
            private readonly static XElement root;
            /// <summary>
            /// Статическое создание
            /// </summary>
            static HatchPatternLoader()
            {
                root = XDocument.Load(System.IO.Path.Combine(Constants.SupportPath, "Pattern.conf.xml")).Element("AcadPatterns");
                cache = new Dictionary<string, Dictionary<string, string>>();
            }
            /// <summary>
            /// Загрузка параметров штриховки
            /// </summary>
            /// <param name="settings">Параметры отрисовки</param>
            /// <returns>Параметры штриховки</returns>
            public static IDictionary<string, string> Load(JObject settings)
            {
                const string BITMAP_NAME = "BitmapName";
                const string BITMAP_INDEX = "BitmapIndex";

                var bitmapName = (settings.Value<string>(BITMAP_NAME) ?? string.Empty).Replace('!', '-');
                var bitmapIndex = settings.Value<int>(BITMAP_INDEX);

                if (cache.TryGetValue(bitmapName+bitmapIndex.ToString(), out var dictionary))
                    return dictionary;

                dictionary = new Dictionary<string, string>();
                var args = root.Element(bitmapName).Element($"t{bitmapIndex}").Value.Trim().Split('\n');
                foreach (var param in args)
                {
                    var arg = param.Split('=');
                    dictionary.Add(arg[0].TrimStart(), arg[1]);
                }

                return dictionary;
            }
        }
    }
    namespace Wkt
    {
        static class Lines
        {
            readonly static Regex line;
            readonly static Regex point;
            static Lines()
            {
                line = new Regex(@"\((\d+(\.\d{0,3})? \d+(\.\d{0,3})?,( ?))+\d+(\.\d{0,3})? \d+(\.\d{0,3})?\)");
                point = new Regex(@"\d+(\.\d{0,3})? \d+(\.\d{0,3})?");
            }
            public static APolyline[] Parse(string wkt)
            {
                var matches = line.Matches(wkt);
                var lines = new APolyline[matches.Count];

                for (int i = 0; i < lines.Length; ++i)
                {
                    lines[i] = new APolyline();
                    var match = point.Matches(matches[i].Value);

                    for (int j = 0; j < match.Count; ++j)
                    {
                        var coords = match[j].Value.Split(' ');
                        lines[i].AddVertexAt(j, new Point2d(coords[0].ToDouble()
                            * Constants.SCALE, coords[1].ToDouble() * Constants.SCALE), 0, 0, 0);
                    }

                }

                return lines;
            }
            public static Point3d ParsePoint(string wkt)
            {
                var coords = point.Match(wkt).Value.Split(' ');

                return new Point3d(coords[0].ToDouble() * Constants.SCALE,
                                   coords[1].ToDouble() * Constants.SCALE,
                                   0);
            }
        }
    }
}