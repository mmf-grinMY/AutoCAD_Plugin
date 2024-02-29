using System;

using APolyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;

using Newtonsoft.Json.Linq;

using static Plugins.Constants;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Plugins.Entities
{
    /// <summary>
    /// Методы расширения для класса Autodesk.AutoCAD.DatabaseServices.Entity
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// Добавить определение поля в таблицу символов
        /// </summary>
        /// <param name="regAppName">Имя поля</param>
        private static void AddRegAppTableRecord(string regAppName)
        {
            var db = Application.DocumentManager.MdiActiveDocument.Database;
            using (var transaction = db.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(db.RegAppTableId, OpenMode.ForWrite) as RegAppTable;
                if (!table.Has(regAppName))
                {
                    var record = new RegAppTableRecord { Name = regAppName };
                    table.Add(record);
                    transaction.AddNewlyCreatedDBObject(record, true);
                }
                transaction.Commit();
            }
        }
        /// <summary>
        /// Добавление в XData необходимых для связывания таблиц параметров
        /// </summary>
        /// <param name="entity">Связываемый объект</param>
        /// <param name="drawParams">Параметры отрисовки</param>
        public static void AddXData(this Autodesk.AutoCAD.DatabaseServices.Entity entity, DrawParams drawParams)
        {
            AddRegAppTableRecord(SYSTEM_ID);
            AddRegAppTableRecord(BASE_NAME);
            AddRegAppTableRecord(LINK_FIELD);

            var buffer = new ResultBuffer(new TypedValue(1001, SYSTEM_ID),
                                                   new TypedValue((int)DxfCode.ExtendedDataInteger32, drawParams.SystemId));

            if (drawParams.LinkedDBFields != null)
            {
                buffer = new ResultBuffer(new TypedValue(1001, SYSTEM_ID),
                                          new TypedValue((int)DxfCode.ExtendedDataInteger32,
                                                         drawParams.SystemId),
                                          new TypedValue(1001, BASE_NAME),
                                          new TypedValue((int)DxfCode.ExtendedDataAsciiString,
                                                         drawParams.LinkedDBFields.BaseName),
                                          new TypedValue(1001, LINK_FIELD),
                                          new TypedValue((int)DxfCode.ExtendedDataAsciiString,
                                                         drawParams.LinkedDBFields.LinkedField));
            }

            entity.XData = buffer;
        }
        /// <summary>
        /// Установка свойств для объекта Polyline
        /// </summary>
        /// <param name="polyline">Исходный объект</param>
        /// <param name="settings">Параметры отрисовки</param>
        /// <param name="layer">Слой отрисовки</param>
        public static APolyline SetDrawSettings(this APolyline polyline, JObject settings, string layer)
        {
            const string PEN_COLOR = "PenColor";
            const string WIDTH = "Width";
            const string BORDER_DESCRIPRION = "BorderDescription";

            polyline.Color = ColorConverter.FromMMColor(settings.Value<int>(PEN_COLOR));
            polyline.Thickness = settings.Value<double>(WIDTH);
            polyline.Layer = layer;

            if (settings.TryGetValue(BORDER_DESCRIPRION, StringComparison.CurrentCulture, out JToken borderDescription)
               && borderDescription.Value<string>() == "{D075F160-4C94-11D3-A90B-A8163E53382F}")
            {
                // FIXME: ??? Данные линии не должны существовать ???
                polyline.Linetype = LineTypeLoader.STYLE_NAME + "2";
                polyline.Color = Color.FromRgb(0, 255, 0);
            }
            else if (settings.Value<int>("nPenStyle") == 1)
            {
                polyline.Linetype = LineTypeLoader.STYLE_NAME + "1";
            }

            return polyline;
        } 
    }
}