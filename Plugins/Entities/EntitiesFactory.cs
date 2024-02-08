using System;

using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins.Entities
{
    /// <summary>
    /// Создатель объектов отрисовки
    /// </summary>
    class EntitiesFactory : IDisposable
    {
#if DEBUG
        /// <summary>
        /// Счетчик ошибок
        /// </summary>
        private readonly ErrorCounter counter;
        /// <summary>
        /// Количество полностью отрисованных объектов
        /// </summary>
        public int Counter => counter.Counter;
        /// <summary>
        /// Количество объектов, при отрисовке которых произошла ошибка
        /// </summary>
        public int Error => counter.Error;
#endif
        /// <summary>
        /// Создание объекта
        /// </summary>
        public EntitiesFactory()
        {
#if DEBUG
            counter = new ErrorCounter();
#endif
        }
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
            switch (draw.DrawSettings.Value<string>("DrawType"))
            {
                case "Polyline":
                    {
                        switch (draw.Geometry)
                        {
                            case Aspose.Gis.Geometries.MultiLineString _: return new Polyline(db, draw, box
#if DEBUG
                                , counter
#endif
                                );
                            case Aspose.Gis.Geometries.Polygon _: return new Polygon(db, draw, box
#if DEBUG
                                , counter
#endif
                                );
                            default: throw new NotImplementedException("При отрисовке полилинии произошла ошибка!");
                        }
                    }
                case "BasicSignDrawParams":
                case "TMMTTFSignDrawParams":
                    {
                        if (draw.Geometry is Aspose.Gis.Geometries.Point point)
                        {
                            return new Sign(db, draw, box
#if DEBUG
                                , counter
#endif
                                );
                        }
                        else
                        {
                            throw new NotImplementedException("При отрисовке знака произошла ошибка!");
                        }
                    }
                case "LabelDrawParams":
                    {
                        if (draw.Geometry is Aspose.Gis.Geometries.Point point)
                        {
                            return new Text(db, draw, box
#if DEBUG
                                , counter
#endif
                                );
                        }
                        else
                        {
                            throw new NotImplementedException("При отрисовке подписи произошла ошибка!");
                        }
                    }
                // FIXME: Возможно более логичным будет при обнаружении нового типа просто пропускать данный объект
                default: throw new ArgumentException("Неизвестный тип рисуемого объекта");
            }
        }
        /// <summary>
        /// Освобождение ресурсов объекта
        /// </summary>
        public void Dispose()
        {
#if DEBUG
            counter.Dispose();
#endif
        }
    }
}