using System;

using Aspose.Gis.Geometries;

using Newtonsoft.Json.Linq;

using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins
{
    class MMPPolygon : MMPPolyline
    {
        public MMPPolygon(Database db, DrawParams draw, Box box) : base(db, draw, box) { }
        public override void DrawLogic(Transaction transaction, BlockTableRecord record)
        {
            using (var polyline = new Autodesk.AutoCAD.DatabaseServices.Polyline())
            {
                try
                {
                    var polygon = drawParams.Geometry as Polygon
                        ?? throw new ArgumentNullException("Невозможно преобразовать данный обхект в тип " + nameof(Polygon), nameof(drawParams.Geometry));
                    var line = polygon.ReplacePolygonsByLines() as LineString ?? throw new ArgumentNullException(nameof(polygon));
                    ActionDrawPolyline(polyline, transaction, record, line);
                    var region = Autodesk.AutoCAD.DatabaseServices.Region.CreateFromCurves(new DBObjectCollection { polyline })[0]
                        as Autodesk.AutoCAD.DatabaseServices.Region ?? throw new ArgumentNullException(nameof(polyline));
                    var color = ColorConverter.FromMMColor(drawParams.DrawSettings.Value<int>(brushColor)); ;
                    region.Color = color;
                    region.Layer = drawParams.LayerName;
                    record.AppendEntity(region);
                    transaction.AddNewlyCreatedDBObject(region, true);
                    var objIdCollection = new ObjectIdCollection { region.ObjectId };

                    // TODO: Переделать логику взятия отрисовки
                    // Описание необходимых заливок имеется в файле $MMPAth\UserData\Pattern.conf
                    using (var hatch = new Hatch())
                    {
                        record.AppendEntity(hatch);
                        transaction.AddNewlyCreatedDBObject(hatch, true);

                        // TODO: Переделать алгоритм выбора заливки
                        string pattern = drawParams.DrawSettings["BitmapName"].Value<string>() + drawParams.DrawSettings["BitmapIndex"].Value<string>();

                        // FIXME: Добавить оригинальную щаливку с этим номером
                        if (pattern != "DRO3247")
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
                catch (ArgumentNullException)
                {
                    // TODO: Предусмотреть перерисовку объекта своими методами
                }
                catch (ArgumentException)
                {
                    // TODO: Предусмотреть перерисовку объекта своими методами
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