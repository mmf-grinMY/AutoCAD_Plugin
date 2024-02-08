using System;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using static Plugins.Constants;

namespace Plugins.Entities
{
    /// <summary>
    /// Подпись
    /// </summary>
    class Text : Entity
    {
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="db">Внутренняя база данных AutoCAD</param>
        /// <param name="draw">Параметры отрисовки</param>
        /// <param name="box">Общий для всех рисуемых объектов BoundingBox</param>
        /// <param name="counter">Счетчик ошибок</param>
        public Text(Database db, DrawParams draw, Box box
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
            int fontSize = drawParams.DrawSettings.Value<int>("FontSize");
            string textString = drawParams.DrawSettings.Value<string>("Text");
            using (var text = new DBText())
            {
                var point = drawParams.Geometry as Aspose.Gis.Geometries.Point
                ?? throw new ArgumentNullException($"Не удалось преобразовать объект {drawParams.Geometry} в тип {nameof(Aspose.Gis.Geometries.Point)}");
                Point3d position = new Point3d(point.X * Scale, point.Y * Scale, 0);
                text.AddXData(drawParams);
                text.SetDatabaseDefaults();
                text.Position = position;
                if (fontSize >= 0)
                    text.Height = fontSize * Scale;
                else
                    throw new ArgumentException($"Невозможно задать тексту шрифт {fontSize} {drawParams.Geometry.AsText()}");
                text.TextString = textString;
                text.Layer = drawParams.LayerName;
                const string ANGLE = "Angle";
                if (drawParams.DrawSettings.ContainsKey(ANGLE))
                    text.Rotation = Convert.ToDouble(drawParams.DrawSettings.Value<string>(ANGLE).Replace('_', ',')) / 180 * Math.PI;
                text.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 0, 0);

                CheckBounds(text);

                record.AppendEntity(text);
                transaction.AddNewlyCreatedDBObject(text, true);
            }
        }
    }
}