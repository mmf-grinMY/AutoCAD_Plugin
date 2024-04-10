using System.Collections.Generic;
using System.Xml.Linq;

using Newtonsoft.Json.Linq;

namespace Plugins
{
    /// <summary>
    /// Загрузчик параметров паттернов штриховки
    /// </summary>
    class HatchPatternLoader : IHatchLoad
    {
        #region Private Fields

        /// <summary>
        /// Кэш параметров заливок
        /// </summary>
        readonly Dictionary<string, Dictionary<string, string>> cache;

        #endregion

        #region Private Static Fields

        /// <summary>
        /// Корневой узел файла конфигурации паттернов штриховки
        /// </summary>
        static XElement root;

        #endregion

        #region Ctor

        /// <summary>
        /// Создание объекта
        /// </summary>
        public HatchPatternLoader()
        {
            if (root is null)
                root = XDocument.Load(System.IO.Path.Combine(Constants.AssemblyPath, "Pattern.xml")).Element("AcadPatterns");

            cache = new Dictionary<string, Dictionary<string, string>>();
        }

        #endregion

        #region Public Methods

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
            // Для DRO32!30 не существует конфигурации заливки
            // TODO: Если не найдены конфигурации, искать файл с паттерном заливки
            var name = root.Element(bitmapName);
            if (name is null) return new Dictionary<string, string>() { { "PatName", bitmapName.Replace('-', '!') + bitmapIndex } };
            var index = name.Element($"t{bitmapIndex}");
            if (index is null) return new Dictionary<string, string>() { { "PatName", bitmapName.Replace('-', '!') + bitmapIndex } };
            var args = index.Value.Trim().Split('\n');

            foreach (var param in args)
            {
                var arg = param.Split('=');
                dictionary.Add(arg[0].TrimStart(), arg[1]);
            }

            return dictionary;
        }

        #endregion
    }
    interface IHatchLoad
    {
        IDictionary<string, string> Load(JObject settings);
    }
}