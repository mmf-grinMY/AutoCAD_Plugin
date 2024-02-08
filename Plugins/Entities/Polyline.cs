using System;
using System.Linq;

using Aspose.Gis.Geometries;

using APolyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;

using static Plugins.Constants;

namespace Plugins.Entities
{
    /// <summary>
    /// Полилиния
    /// </summary>
    class Polyline : Entity
    {
        protected const string brushColor = "BrushColor";
        protected const string brushBkColor = "BrushBkColor";
        protected const string width = "width";
        /// <summary>
        /// Внутренняя логика отрисовки линии
        /// </summary>
        /// <param name="polyline">Отрисовываемыя в AutoCAD линия</param>
        /// <param name="transaction">Транзакция во внутреннюю базу данных AutoCAD</param>
        /// <param name="record">Запись в таблицу блоков</param>
        /// <param name="line">Описание линии</param>
        protected void ActionDrawPolyline(APolyline polyline, Transaction transaction, BlockTableRecord record, LineString line)
        {
            for (int i = 0; i < line.Count; i++)
            {
                var point = new Point2d(line[i].X * Scale, line[i].Y * Scale);
                polyline.AddVertexAt(i, point, 0, 0, 0);
            }

            polyline.Color = ColorConverter.FromMMColor(drawParams.DrawSettings.Value<int>(brushColor));
            polyline.Thickness = drawParams.DrawSettings.Value<double>(width);
            polyline.Layer = drawParams.LayerName;

            CheckBounds(polyline);
            record.AppendEntity(polyline);
            transaction.AddNewlyCreatedDBObject(polyline, true);
        }
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="db">Внутренняя база данных AutoCAD</param>
        /// <param name="draw">Параметры отрисовки</param>
        /// <param name="box">Общий для всех рисуемых объектов BoundingBox</param>
        /// <param name="counter">Счетчик ошибок</param>
        public Polyline(Database db, DrawParams draw, Box box
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
            MultiLineString lines = drawParams.Geometry as MultiLineString ?? throw new ArgumentException("Объект не является типом MultiLineString", nameof(drawParams.Geometry));
            foreach (LineString line in lines.Cast<LineString>())
            {
                using (var polyline = new APolyline())
                {
                    polyline.AddXData(drawParams);
                    ActionDrawPolyline(polyline, transaction, record, line);
                    // TODO: Добавить выбор типа линии
                    if (drawParams.DrawSettings.ContainsKey("BorderDescription"))
                    {
                        if (drawParams.DrawSettings.Value<string>("BorderDescription") == "{D075F160-4C94-11D3-A90B-A8163E53382F}")
                        {
                            polyline.Linetype = "Contur";
                            polyline.Color = Color.FromRgb(0, 128, 0);
                        }
                    }
                }
            }
        }
    }
}