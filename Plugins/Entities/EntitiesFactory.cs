using Plugins.Dispatchers;

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
        readonly IHatchLoad hatchPatternLoader;
        /// <summary>
        /// Фабрика блоков
        /// </summary>
        readonly ITableDispatcher factory;
        /// <summary>
        /// Диспетчер работы с БД
        /// </summary>
        readonly IDbDispatcher dispatcher;

        #endregion

        #region Ctor

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="creater">Создатель блоков</param>
        public EntitiesFactory(ITableDispatcher creater, IDbDispatcher dispatcher)
        {
            factory = creater ?? throw new ArgumentNullException(nameof(creater));
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
        /// <exception cref="ArgumentException">Возникает при отрисовке неизвестных геометрий</exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotImplementedException">Возникает при невозможности отрисовки объектов типа Polyline</exception>
        public Entity Create(Primitive primitive)
        {
            if (primitive is null)
                throw new ArgumentNullException(nameof(primitive));

            switch (primitive.DrawSettings.Value<string>("DrawType"))
            {
                case "Polyline":
                    if (primitive.Geometry.StartsWith("MULTILINESTRING"))
                    {
                        return new Polyline(primitive, dispatcher);
                    }
                    else if (primitive.Geometry.StartsWith("POLYGON"))
                    {
                        return new Polygon(primitive, hatchStyle, hatchPatternLoader, dispatcher);
                    }
                    else
                    {
                        throw new NotImplementedException($"При отрисовке полилинии {primitive.Guid} произошла ошибка!");
                    }
                case "BasicSignDrawParams":
                case "TMMTTFSignDrawParams": return new Sign(primitive, factory, signStyle);
                case "LabelDrawParams": return new Text(primitive, textStyle);
                default: throw new ArgumentException("Неизвестный тип рисуемого объекта");
            }
        }

        #endregion
    }
}