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
        /// Создать объект отрисовки
        /// </summary>
        /// <param name="db">Внутренняя база данных AutoCAD</param>
        /// <param name="draw">Параметры отрисовки</param>
        /// <param name="box">Общий BoundingBox всех рисуемых объектов</param>
        /// <returns>Объект отрисовки</returns>
        /// <exception cref="NotImplementedException">Возникает при отрисовке неизвестных геометрий</exception>
        public Entity Create(Database db, DrawParams draw, Box box )
        {
            switch (draw.DrawSettings.GetProperty("DrawType").GetString())
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
                case "TMMTTFSignDrawParams":
                    {
                        return new Sign(db, draw, box);
                    }
                case "LabelDrawParams":
                    {
                        return new Text(db, draw, box);
                    }
                // FIXME: Возможно более логичным будет при обнаружении нового типа просто пропускать данный объект
                default: throw new ArgumentException("Неизвестный тип рисуемого объекта");
            }
        }
    }
}