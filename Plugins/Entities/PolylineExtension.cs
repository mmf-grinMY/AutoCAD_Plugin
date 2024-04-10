using Autodesk.AutoCAD.DatabaseServices;

using Newtonsoft.Json.Linq;

using System;

namespace Plugins.Entities
{
    static class PolylineExtension
    {
        /// <summary>
        /// Установка свойств для объекта Polyline
        /// </summary>
        /// <param name="polyline">Исходный объект</param>
        /// <param name="settings">Параметры отрисовки</param>
        /// <param name="layer">Слой отрисовки</param>
        public static Autodesk.AutoCAD.DatabaseServices.Polyline SetDrawSettings(this Autodesk.AutoCAD.DatabaseServices.Polyline polyline,
                                                                                 JObject settings,
                                                                                 string layer)
        {
            const string PEN_COLOR = "PenColor";
            const string WIDTH = "Width";
            const string BORDER_DESCRIPRION = "BorderDescription";
            const string PEN_STYLE = "nPenStyle";

            const string NOT_DRAWING_LINE_STYLE = "{D075F160-4C94-11D3-A90B-A8163E53382F}";

            polyline.Color = ColorConverter.FromMMColor(settings.Value<int>(PEN_COLOR));
            polyline.Thickness = settings.Value<double>(WIDTH);
            polyline.Layer = layer;

            // TODO: Добавить загрузку штриховок из файла стилей линий
            if (settings.TryGetValue(BORDER_DESCRIPRION, StringComparison.CurrentCulture, out JToken borderDescription)
               && borderDescription.Value<string>() == NOT_DRAWING_LINE_STYLE)
            {
                throw new NotDrawingLineException();
            }
            else if (settings.Value<int>(PEN_STYLE) == 1)
            {
                // TODO: Исправить это явное вхождение названия штриховки
                polyline.Linetype = "MMLT_1";
            }

            return polyline;
        }
        public static void Append(this Autodesk.AutoCAD.DatabaseServices.Polyline polyline,
                                 Transaction transaction,
                                 BlockTableRecord record,
                                 Primitive primitive)
        {
            polyline
                .SetDrawSettings(primitive.DrawSettings, primitive.LayerName)
                .AppendToDb(transaction, record, primitive);
        }
    }
}