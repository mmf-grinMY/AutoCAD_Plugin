using System;
using System.Xml.Linq;
using System.Text.Json;
using System.Collections.Generic;

using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

using static Plugins.Constants;

namespace Plugins.Entities
{
    /// <summary>
    /// Методы расширения для класса Autodesk.AutoCAD.DatabaseServices.Entity
    /// </summary>
    public static class EntityExtensions
    {
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
                                          new TypedValue((int)DxfCode.ExtendedDataInteger32, drawParams.SystemId),
                                          new TypedValue(1001, BASE_NAME),
                                          new TypedValue((int)DxfCode.ExtendedDataAsciiString, drawParams.LinkedDBFields.BaseName),
                                          new TypedValue(1001, LINK_FIELD),
                                          new TypedValue((int)DxfCode.ExtendedDataAsciiString, drawParams.LinkedDBFields.LinkedField));
            }

            entity.XData = buffer;
        }
        public static void SetDrawSettings(this Autodesk.AutoCAD.DatabaseServices.Polyline polyline, DrawParams drawParams)
        {
            const string brushBkColor = "BrushBkColor";
            const string width = "Width";

            polyline.Color = ColorConverter.FromMMColor(drawParams.DrawSettings.GetProperty(brushBkColor).GetInt32());
            polyline.Thickness = drawParams.DrawSettings.GetProperty(width).GetDouble();
            polyline.Layer = drawParams.LayerName;

            // TODO: Добавить выбор типа линии
            if (drawParams.DrawSettings.TryGetProperty("BorderDescription", out JsonElement borderDescription))
            {
                if (borderDescription.GetString() == "{D075F160-4C94-11D3-A90B-A8163E53382F}")
                {
                    polyline.Linetype = "Contur";
                    polyline.Color = Color.FromRgb(0, 128, 0);
                }
            }
        }
        public static void SetDrawSettings(this Hatch hatch, DrawParams drawParams)
        {
            // TODO: Хранить в кеше уже прочитанные параметры отрисовки
            var root = XDocument.Load(System.IO.Path.Combine(Constants.SupportPath, "Pattern.conf.xml")).Element("AcadPatterns");

            var dictionary = new Dictionary<string, string>();
            var bitmapName = (drawParams.DrawSettings.GetProperty("BitmapName").GetString() ?? string.Empty).Replace('!', '-');
            var bitmapIndex = drawParams.DrawSettings.GetProperty("BitmapIndex").GetInt32();
            var args = root.Element(bitmapName).Element($"t{bitmapIndex}").Value.Trim().Split('\n');
            foreach (var param in args)
            {
                var arg = param.Split('=');
                dictionary.Add(arg[0].TrimStart(), arg[1]);
            }

            double GetValue(string key)
            {
                return Convert.ToDouble(dictionary[key].Replace('.', ','));
            }

            const string SOLID = "SOLID";
            const string PAT_NAME = "PatName";
            const string PAT_ANGLE = "PatAngle";
            const string PAT_SCALE = "PatScale";
            const string brushColor = "BrushColor";

            var name = dictionary[PAT_NAME];
            if (name == SOLID)
            {
                hatch.SetHatchPattern(HatchPatternType.PreDefined, name);
            }
            else
            {
                //hatch.SetHatchPattern(HatchPatternType.CustomDefined, name);
            }
            hatch.HatchObjectType = HatchObjectType.HatchObject;
            if (dictionary.ContainsKey(PAT_SCALE))
            {
                hatch.PatternScale = GetValue(PAT_SCALE) * Constants.SCALE;
            }
            if (dictionary.ContainsKey(PAT_ANGLE))
            {
                hatch.PatternAngle = GetValue(PAT_ANGLE) / 180 * Math.PI;
            }

            hatch.Color = ColorConverter.FromMMColor(drawParams.DrawSettings.GetProperty(brushColor).GetInt32());
            hatch.Layer = drawParams.LayerName;
            hatch.SetDatabaseDefaults();
        }
    }
}