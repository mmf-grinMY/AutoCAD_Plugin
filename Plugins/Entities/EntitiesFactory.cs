using Plugins.Dispatchers;
using Plugins.Logging;

using System;

namespace Plugins.Entities
{
    /// <summary>
    /// Создатель объектов отрисовки
    /// </summary>
    class EntitiesFactory
    {
        /// <summary>
        /// Текущий логер событий
        /// </summary>
        readonly ILogger logger;
        /// <summary>
        /// Фабрика блоков
        /// </summary>
        readonly SymbolTableDispatcher factory;
        /// <summary>
        /// Создание объекта
        /// </summary>
        public EntitiesFactory(SymbolTableDispatcher creater, ILogger log)
        {
            factory = creater;
            logger = log;
        }
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
                        return new Polygon(primitive, logger);
                    }
                    else
                    {
                        throw new NotImplementedException("При отрисовке полилинии произошла ошибка!");
                    }
                case "BasicSignDrawParams":
                case "TMMTTFSignDrawParams": return new Sign(primitive, logger, factory);
                case "LabelDrawParams": return new Text(primitive, logger);
                default: throw new ArgumentException("Неизвестный тип рисуемого объекта");
            }
        }
    }
}