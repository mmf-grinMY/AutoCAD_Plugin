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
        public Entity Create(DrawParams draw)
        {
            switch (draw.DrawSettings.Value<string>("DrawType"))
            {
                case "Polyline":
                    {
                        switch (draw.Geometry)
                        {
                            case Aspose.Gis.Geometries.MultiLineString _: return new Polyline(db, draw, box);
                            case Aspose.Gis.Geometries.Polygon _: return new Polygon(db, draw, box);
                            default: throw new NotImplementedException("При отрисовке полилинии произошла ошибка!");
                        }
                    }
                case "BasicSignDrawParams":
                case "TMMTTFSignDrawParams": return new Sign(db, draw, box);
                case "LabelDrawParams": return new Text(db, draw, box);
                // FIXME: ??? Возможно более логичным будет при обнаружении нового типа просто пропускать данный объект
                default: throw new ArgumentException("Неизвестный тип рисуемого объекта");
            }
        }
    }
}