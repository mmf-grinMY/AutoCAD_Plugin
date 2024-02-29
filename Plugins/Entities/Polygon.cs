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
        /// <param name="box">Общий для всех рисуемых объектов BoundingBox</param>
        public Polygon(Database db, Primitive draw, Box box) : base(db, draw, box) { }
        /// <summary>
        /// Создание примитива 
        /// </summary>
        /// <param name="polygon">Исходный полигон</param>
        /// <returns>Примитив</returns>
        private static APolyline Create(Aspose.Gis.Geometries.Polygon polygon)
        {
            var polyline = new APolyline();

            int i = 0;
            foreach (var p in polygon.ExteriorRing)
            {
                var point = new Point2d(p.X * Constants.SCALE, p.Y * Constants.SCALE);
                polyline.AddVertexAt(i, point, 0, 0, 0);
                ++i;
            }

            return polyline;
        }
        /// <summary>
        /// Рисование объекта
        /// </summary>
        public override void Draw()
        {
            using (var polyline = Create(drawParams.Geometry as Aspose.Gis.Geometries.Polygon))
            {
                AppendToDb(polyline.SetDrawSettings(drawParams.DrawSettings, drawParams.LayerName));
                using (var hatch = new Hatch())
                {
                    AppendToDb(hatch);

                    var dictionary = HatchPatternLoader.Load(drawParams.DrawSettings);

                    double GetValue(string key) => dictionary.ContainsKey(key) ? dictionary[key].ToDouble() : 1;

                    const string PAT_NAME = "PatName";
                    const string PAT_ANGLE = "PatAngle";
                    const string PAT_SCALE = "PatScale";
                    const string brushColor = "BrushColor";

                    // FIXME: Добавить поддержку свойства ForeColor
                    // На горизонте K450E нет заливок, требующих это свойство
                    hatch.PatternScale = Constants.HATCH_SCALE * GetValue(PAT_SCALE);
                    hatch.SetHatchPattern(HatchPatternType.PreDefined, dictionary[PAT_NAME]);

                    if (dictionary.TryGetValue(PAT_ANGLE, out var angle))
                    {
                        hatch.PatternAngle = angle.ToDouble().ToRad();
                    }

                    hatch.Associative = true;
                    hatch.Color = ColorConverter.FromMMColor(drawParams.DrawSettings.Value<int>(brushColor));
                    hatch.Layer = drawParams.LayerName;

                    hatch.AppendLoop(HatchLoopTypes.Outermost, new ObjectIdCollection { polyline.ObjectId });
                    hatch.EvaluateHatch(true);
                }
            }
        }
        /// <summary>
        /// Загрузчик параметров паттернов штриховки
        /// </summary>
        private static class HatchPatternLoader
        {
            /// <summary>
            /// Кэш параметров заливок
            /// </summary>
            private static Dictionary<string, Dictionary<string, string>> cache;
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
}