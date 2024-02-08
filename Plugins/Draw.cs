namespace Plugins
{
    /// <summary>
    /// Строковые параметры отрисовки
    /// </summary>
    public readonly struct Draw
    {
        #region Public Properties
        /// <summary>
        /// Геометрия объекта в формате WKT
        /// </summary>
        public string WKT { get; }
        /// <summary>
        /// Параметры легендаризации
        /// </summary>
        public string DrawSettings { get; }
        /// <summary>
        /// Общие параметры отрисовки
        /// </summary>
        public string Param { get; }
        /// <summary>
        /// Имя слоя
        /// </summary>
        public string Layername { get; }
        public string SystemId { get; }
        public LinkedDBFields LinkedFields { get; }
        #endregion

        #region Ctors
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="wkt">Геометрия в формате WKT</param>
        /// <param name="draw">Легендаризация объекта</param>
        /// <param name="param">Общие параметры</param>
        /// <param name="layername">Имя слоя</param>
        public Draw(string wkt, string draw, string param, string layername, string systemId, LinkedDBFields linkedFields)
        {
            WKT = wkt;
            DrawSettings = draw;
            Param = param;
            Layername = layername;
            SystemId = systemId;
            LinkedFields = linkedFields;
        }
        #endregion
    }
    public class LinkedDBFields
    {
        public string BaseName { get; }
        public string LinkedField { get; }
        public LinkedDBFields(string bseName, string linkedField)
        {
            BaseName = bseName;
            LinkedField = linkedField;
        }
    }
}