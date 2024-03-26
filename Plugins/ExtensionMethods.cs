using System.Collections.Generic;
using System;

using Autodesk.AutoCAD.DatabaseServices;

using Newtonsoft.Json.Linq;

using static Plugins.Constants;

namespace Plugins
{
    static class ExtensionMethods
    {
        /// <summary>
        /// Конвертация строки в вещественное число
        /// </summary>
        /// <param name="str">Строковое представление числа</param>
        /// <returns>Вещественное число</returns>
        public static double ToDouble(this string str)
        {
            if (str is null)
                return 0.0;

            if (str.Contains("_"))
                str = str.Replace('_', '.');
            else if (str.Contains(","))
                str = str.Replace(',', '.');

            return double.Parse(str, System.Globalization.CultureInfo.GetCultureInfo("en"));
        }
        /// <summary>
        /// Конвертация градусов в радианы
        /// </summary>
        /// <param name="degree">Угол в градусах</param>
        /// <returns>Угол в радианах</returns>
        public static double ToRad(this double degree) => degree / 180 * System.Math.PI;
        
        /// <summary>
        /// Добавление в XData необходимых для связывания таблиц параметров
        /// </summary>
        /// <param name="entity">Связываемый объект</param>
        /// <param name="primitive">Параметры отрисовки</param>
        public static void AddXData(this Entity entity, Entities.Primitive primitive)
        {
            var typedValues = new List<TypedValue>
            {
                new TypedValue(1001, SYSTEM_ID), 
                new TypedValue((int)DxfCode.ExtendedDataInteger32, primitive.SystemId),
                new TypedValue(1001, OBJ_ID),
                new TypedValue((int)DxfCode.ExtendedDataInteger32, primitive.Id),
            };

            if (primitive.BaseName != null && primitive.ChildField != null)
            {
                typedValues.Add(new TypedValue(1001, BASE_NAME));
                typedValues.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, primitive.BaseName));
                typedValues.Add(new TypedValue(1001, LINK_FIELD));
                typedValues.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, primitive.ChildField));
            }

            entity.XData = new ResultBuffer(typedValues.ToArray());
        }
        /// <summary>
        /// Установка свойств для объекта Polyline
        /// </summary>
        /// <param name="polyline">Исходный объект</param>
        /// <param name="settings">Параметры отрисовки</param>
        /// <param name="layer">Слой отрисовки</param>
        public static Polyline SetDrawSettings(this Polyline polyline, JObject settings, string layer)
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
                polyline.Linetype = Commands.TYPE_NAME;
            }

            return polyline;
        }
        /// <summary>
        /// Получение XData
        /// </summary>
        /// <param name="buffer">Исходный буфер</param>
        /// <param name="RegAppName">Зарегестрированное имя</param>
        /// <returns>Хранимые данные</returns>
        public static string GetXData(this ResultBuffer buffer, string RegAppName)
        {
            for (var iter = buffer.GetEnumerator(); iter.MoveNext();)
            {
                var tv = iter.Current;

                if ((tv.TypeCode == (short)DxfCode.ExtendedDataRegAppName) && (tv.Value.ToString() == RegAppName))
                {
                    if (iter.MoveNext())
                    {
                        return iter.Current.Value.ToString();
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return string.Empty;
        }
        /// <summary>
        /// Добавить примитив в БД AutoCAD
        /// </summary>
        /// <param name="entity">Сохраняемый объект</param>
        /// <param name="transaction">Текщуая транзакция</param>
        /// <param name="record">Текщая запись таблицы блоков</param>
        /// <param name="primitive">Примитив объекта</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AppendToDb(this Entity entity,
                                      Transaction transaction,
                                      BlockTableRecord record,
                                      Entities.Primitive primitive)
        {
            if (entity is null)
                throw new ArgumentNullException(nameof(entity));

            if (transaction is null)
                throw new ArgumentNullException(nameof(transaction));

            if (record is null)
                throw new ArgumentNullException(nameof(record));

            if (primitive is null)
                throw new ArgumentNullException(nameof(primitive));

            entity.AddXData(primitive);
            record.AppendEntity(entity);
            transaction.AddNewlyCreatedDBObject(entity, true);
        }
    }
}