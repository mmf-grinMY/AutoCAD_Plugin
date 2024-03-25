using System.Collections.Generic;
using System.Xml.Linq;

using Newtonsoft.Json.Linq;

namespace Plugins
{
    /// <summary>
    /// Загрузчик параметров паттернов штриховки
    /// </summary>
    class HatchPatternLoader
    {
        /// <summary>
        /// Кэш параметров заливок
        /// </summary>
        readonly static Dictionary<string, Dictionary<string, string>> cache;
        /// <summary>
        /// Корневой узел файла конфигурации паттернов штриховки
        /// </summary>
        readonly static XElement root;
        /// <summary>
        /// Статическое создание
        /// </summary>
        static HatchPatternLoader()
        {
            root = XDocument.Load(System.IO.Path.Combine(Constants.SupportPath, "Pattern.conf.xml")).Element("AcadPatterns");
            cache = new Dictionary<string, Dictionary<string, string>>();
        }
        public HatchPatternLoader()
        {
            cache.Clear();
        }
        /// <summary>
        /// Загрузка параметров штриховки
        /// </summary>
        /// <param name="settings">Параметры отрисовки</param>
        /// <returns>Параметры штриховки</returns>
        public IDictionary<string, string> Load(JObject settings)
        {
            const string BITMAP_NAME = "BitmapName";
            const string BITMAP_INDEX = "BitmapIndex";

            var bitmapName = settings.Value<string>(BITMAP_NAME);

            if (string.IsNullOrEmpty(bitmapName)) return null;

            bitmapName = bitmapName.Replace('!', '-');
            var bitmapIndex = settings.Value<int>(BITMAP_INDEX);

            if (cache.TryGetValue(bitmapName + bitmapIndex.ToString(), out var dictionary))
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