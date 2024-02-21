using System;
using System.Text.Json;

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
        public Text(Database db, DrawParams draw, Box box) : base(db, draw, box) { }
        public override void Draw()
        {
            var settings = drawParams.DrawSettings;
            var fontSize = settings.GetProperty("FontSize").GetInt32() * SCALE;

            var point = drawParams.Geometry as Aspose.Gis.Geometries.Point
                ?? throw new ArgumentNullException($"Не удалось преобразовать объект {drawParams.Geometry} в тип {nameof(Aspose.Gis.Geometries.Point)}");

            using (var text = new DBText()
            {
                TextString = settings.GetProperty("Text").GetString(),
                Layer = drawParams.LayerName,
                Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 0, 0),
                Position = new Point3d(point.X * SCALE, point.Y * SCALE, 0),
                Height = fontSize > 0 
                    ? fontSize 
                    : throw new ArgumentException($"Невозможно задать тексту шрифт {fontSize} {drawParams.Geometry.AsText()}")
            })
            {
                // TODO: Проверить параметры установки поворота текста
                if (settings.TryGetProperty("Angle", out JsonElement angle))
                    text.Rotation = Convert.ToDouble(angle.GetString().Replace('_', ',')) / 180 * Math.PI;

                AppendToDb(text);
            }
        }
    }
}