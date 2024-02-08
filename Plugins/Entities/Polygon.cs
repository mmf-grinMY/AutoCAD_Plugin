using System;

using Newtonsoft.Json.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using System.Text.RegularExpressions;

namespace Plugins.Entities
{
    // TODO: Переделать иерархию классов
    // Polygon не является линией
    /// <summary>
    /// Полигон
    /// </summary>
    sealed class Polygon : Polyline
    {
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="db">Внутренняя база данных AutoCAD</param>
        /// <param name="draw">Параметры отрисовки</param>
        /// <param name="box">Общий для всех рисуемых объектов BoundingBox</param>
        /// <param name="counter">Счетчик ошибок</param>
        public Polygon(Database db, DrawParams draw, Box box
#if DEBUG
            , ErrorCounter counter
#endif
            ) : base(db, draw, box
#if DEBUG
                , counter
#endif
                ) { }
        protected override void DrawLogic(Transaction transaction, BlockTableRecord record)
        {
            using (var polyline = new Autodesk.AutoCAD.DatabaseServices.Polyline())
            {
                try
                {
                    var polygon = drawParams.Geometry as Aspose.Gis.Geometries.Polygon
                        ?? throw new ArgumentNullException("Невозможно преобразовать данный объект в тип " + nameof(Aspose.Gis.Geometries.Polygon), nameof(drawParams.Geometry));
                    var line = new Aspose.Gis.Geometries.LineString();
                    var ring = polygon.ExteriorRing;
                    foreach (var point in ring)
                    {
                        line.AddPoint(point);
                    }

                    ActionDrawPolyline(polyline, transaction, record, line);
                    var color = ColorConverter.FromMMColor(drawParams.DrawSettings.Value<int>(brushColor));
                    var objIdCollection = new ObjectIdCollection { polyline.ObjectId };

                    // TODO: Переделать логику взятия отрисовки
                    // Описание необходимых заливок имеется в файле $MMPAth\UserData\Pattern.conf
                    using (var hatch = new Hatch())
                    {
                        hatch.AddXData(drawParams);
                        record.AppendEntity(hatch);
                        transaction.AddNewlyCreatedDBObject(hatch, true);

                        // TODO: Переделать алгоритм выбора заливки
                        string pattern = drawParams.DrawSettings["BitmapName"].Value<string>() + drawParams.DrawSettings["BitmapIndex"].Value<string>();

                        // FIXME: Добавить оригинальную заливку с номерами DRO32\d\d
                        if (new Regex(@"DRO32!.*").IsMatch(pattern))
                            hatch.SetHatchPattern(HatchPatternType.CustomDefined, pattern);
                        else
                            hatch.SetHatchPattern(HatchPatternType.UserDefined, "SOLID");
                        hatch.Color = color;
                        hatch.Layer = drawParams.LayerName;
                        hatch.Associative = true;
                        hatch.AppendLoop(HatchLoopTypes.Outermost, objIdCollection);
                        hatch.EvaluateHatch(true);
                    }
                }
                catch (ArgumentNullException ex)
                {
                    // TODO: Предусмотреть перерисовку объекта своими методами

#if DEBUG
                    counter.Log(ex, drawParams.Geometry.AsText());
#endif
                }
                catch (ArgumentException ex)
                {
                    // TODO: Предусмотреть перерисовку объекта своими методами

#if DEBUG
                    counter.Log(ex, drawParams.Geometry.AsText());
#endif
                }
                catch (Exception ex)
                {
                    if (ex.Message != "eInvalidInput")
                    {
                        throw ex;
                    }
                }
            }
        }
    }
}