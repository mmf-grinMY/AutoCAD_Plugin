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
        readonly Database db;
        /// <summary>
        /// Создание объекта
        /// </summary>
        public EntitiesFactory()
        {
            db = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database;
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
                        return new Polyline(db, draw);
                    }
                    else if (draw.Geometry.StartsWith("POLYGON"))
                    {
                        return new Polygon(db, draw);
                    }
                    else
                    {
                        // FIXME: Заменить на логгирование
#if !RELEASE
                        throw new NotImplementedException("При отрисовке полилинии произошла ошибка!");
#else
                        break;
#endif
                    }
                case "BasicSignDrawParams":
                case "TMMTTFSignDrawParams": return new Sign(db, draw);
                case "LabelDrawParams": return new Text(db, draw);
                default:
                    // FIXME: Заменить на логгирование
#if !RELEASE
                    throw new ArgumentException("Неизвестный тип рисуемого объекта");
#else
                    break;
#endif
            }
        }
    }
}