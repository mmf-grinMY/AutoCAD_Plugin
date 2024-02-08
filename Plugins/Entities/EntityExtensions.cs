using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins.Entities
{
    /// <summary>
    /// Методы расширения для класса Autodesk.AutoCAD.DatabaseServices.Entity
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// Добавление в XData необходимых для связывания таблиц параметров
        /// </summary>
        /// <param name="entity">Связываемый объект</param>
        /// <param name="drawParams">Параметры отрисовки</param>
        public static void AddXData(this Autodesk.AutoCAD.DatabaseServices.Entity entity, DrawParams drawParams)
        {
            const string VAR_SYSTEM_ID = "varMM_SystemID";
            const string VAR_BASE_NAME = "varMM_BaseName";
            const string VAR_LINK_FIELD = "varMM_LinkField";
            Commands.AddRegAppTableRecord(VAR_SYSTEM_ID);
            Commands.AddRegAppTableRecord(VAR_BASE_NAME);
            Commands.AddRegAppTableRecord(VAR_LINK_FIELD);

            ResultBuffer buffer = new ResultBuffer(new TypedValue(1001, VAR_SYSTEM_ID),
                                                   new TypedValue((int)DxfCode.ExtendedDataInteger32, drawParams.SystemId));

            if (drawParams.LinkedDBFields != null)
            {
                buffer = new ResultBuffer(new TypedValue(1001, VAR_SYSTEM_ID),
                                          new TypedValue((int)DxfCode.ExtendedDataInteger32, drawParams.SystemId),
                                          new TypedValue(1001, VAR_BASE_NAME),
                                          new TypedValue((int)DxfCode.ExtendedDataAsciiString, drawParams.LinkedDBFields.BaseName),
                                          new TypedValue(1001, VAR_LINK_FIELD),
                                          new TypedValue((int)DxfCode.ExtendedDataAsciiString, drawParams.LinkedDBFields.LinkedField));
            }

            entity.XData = buffer;
        }
    }
}