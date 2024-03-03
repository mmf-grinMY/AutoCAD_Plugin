using System;

using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins.Entities
{
    /// <summary>
    /// Создатель объектов отрисовки
    /// </summary>
    class EntitiesFactory
    {
        /// <summary>
        /// Внутренняя БД AutoCAD
        /// </summary>
        private readonly Database db;
        /// <summary>
        /// Грагичная рамка рисуемых объектов
        /// </summary>
        private readonly Box box;
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="db">Внутренняя база данных AutoCAD</param>
        /// <param name="box">Общий BoundingBox всех рисуемых объектов</param>
        public EntitiesFactory(Database db, Box box)
        {
            this.db = db;
            this.box = box;
        }
        /// <summary>
        /// Создание объекта отрисовки
        /// </summary>
        /// <param name="draw">Параметры отрисовки</param>
        /// <returns>Объект отрисовки</returns>
        /// <exception cref="NotImplementedException">Возникает при отрисовке объектов типа Polyline</exception>
        /// <exception cref="ArgumentException">Возникает при отрисовке неизвестных геометрий</exception>
        public Entity Create(Primitive draw)
        {
            switch (draw.DrawSettings.Value<string>("DrawType"))
            {
                case "Polyline":
                    if (draw.Geometry.StartsWith("MULTILINESTRING"))
                    {
                        return new Polyline(db, draw, box);
                    }
                    else if (draw.Geometry.StartsWith("POLYGON"))
                    {
                        return new Polygon(db, draw, box);
                    }
                    else
                    {
#if !RELEASE
                        throw new NotImplementedException("При отрисовке полилинии произошла ошибка!");
#else
                            break;
#endif
                    }
                case "BasicSignDrawParams":
                case "TMMTTFSignDrawParams": return new Sign(db, draw, box);
                case "LabelDrawParams": return new Text(db, draw, box);
                default:
#if !RELEASE
                    throw new ArgumentException("Неизвестный тип рисуемого объекта");
#else
                    break;
#endif
            }
        }
    }
}