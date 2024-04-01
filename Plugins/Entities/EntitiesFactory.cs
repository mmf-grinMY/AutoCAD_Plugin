using Plugins.Dispatchers;
using Plugins.Logging;

using System.IO;
using System;

using Newtonsoft.Json.Linq;

namespace Plugins.Entities
{
    /// <summary>
    /// Создатель объектов отрисовки
    /// </summary>
    class EntitiesFactory
    {
        #region Private Fields

        /// <summary>
        /// Стиль текста
        /// </summary>
        readonly MyEntityStyle textStyle;
        /// <summary>
        /// Стиль штриховки
        /// </summary>
        readonly MyHatchStyle hatchStyle;
        /// <summary>
        /// Стиль знаков
        /// </summary>
        readonly MyEntityStyle signStyle;
        /// <summary>
        /// Загрузчик штриховок
        /// </summary>
        readonly HatchPatternLoader hatchPatternLoader;
        /// <summary>
        /// Текущий логер событий
        /// </summary>
        readonly ILogger logger;
        /// <summary>
        /// Фабрика блоков
        /// </summary>
        readonly SymbolTableDispatcher factory;
        /// <summary>
        /// Диспетчер работы с БД
        /// </summary>
        readonly OracleDbDispatcher dispatcher;

        #endregion

        #region Ctor

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="creater">Создатель блоков</param>
        /// <param name="log">Логер событий</param>
        public EntitiesFactory(SymbolTableDispatcher creater, ILogger log, OracleDbDispatcher dispatcher)
        {
            factory = creater ?? throw new ArgumentNullException(nameof(creater));
            logger = log ?? throw new ArgumentNullException(nameof(log));
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            hatchPatternLoader = new HatchPatternLoader();

            var config = JObject.Parse(File.ReadAllText(Path.Combine(Constants.AssemblyPath, "style.config.json")));
            var hatch = config.Value<JObject>("hatch");

            textStyle = new MyEntityStyle(config.Value<JObject>("text").Value<double>("scale"));
            hatchStyle = new MyHatchStyle(hatch.Value<double>("scale"), hatch.Value<byte>("transparency"));
            signStyle = new MyEntityStyle(config.Value<JObject>("sign").Value<double>("scale"));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Создание объекта отрисовки
        /// </summary>
        /// <param name="primitive">Параметры отрисовки</param>
        /// <returns>Объект отрисовки</returns>
        /// <exception cref="NotImplementedException">Возникает при невозможности отрисовки объектов типа Polyline</exception>
        /// <exception cref="ArgumentException">Возникает при отрисовке неизвестных геометрий</exception>
        public Entity Create(Primitive primitive)
        {
            switch (primitive.DrawSettings.Value<string>("DrawType"))
            {
                case "Polyline":
                    if (primitive.Geometry.StartsWith("MULTILINESTRING"))
                    {
                        return new Polyline(primitive, logger);
                    }
                    else if (primitive.Geometry.StartsWith("POLYGON"))
                    {
                        return new Polygon(primitive, logger, hatchPatternLoader, hatchStyle, dispatcher);
                    }
                    else
                    {
                        throw new NotImplementedException("При отрисовке полилинии произошла ошибка!");
                    }
                case "BasicSignDrawParams":
                case "TMMTTFSignDrawParams": return new Sign(primitive, logger, factory, signStyle);
                case "LabelDrawParams": return new Text(primitive, logger, textStyle);
                default: throw new ArgumentException("Неизвестный тип рисуемого объекта");
            }
        }

        #endregion
    }
}