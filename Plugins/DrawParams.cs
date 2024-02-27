using System;

using Aspose.Gis.Geometries;

using Newtonsoft.Json.Linq;

namespace Plugins
{
    // FIXME: ??? Нужен ли отдельный класс для хранения | Стоит ли заменить на Tuple? ???
    /// <summary>
    /// Параметры отрисовки
    /// </summary>
    public class DrawParams
    {
        #region Public Properties
        /// <summary>
        /// Геометрический объект
        /// </summary>
        public IGeometry Geometry { get; }
        /// <summary>
        /// Параметры легендаризации
        /// </summary>
        public JObject DrawSettings { get; }
        /// <summary>
        /// Общие параметры отрисовки
        /// </summary>
        public JObject Param { get; }
        /// <summary>
        /// Имя слоя
        /// </summary>
        public string LayerName { get; }
        /// <summary>
        /// Уникальный номер примитива
        /// </summary>
        public int SystemId { get; }
        /// <summary>
        /// Линкованные столбцы
        /// </summary>
        public LinkedDBFields LinkedDBFields { get; }
        #endregion

        #region Ctors
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="draw">Строковые параметры отрисовки</param>
        public DrawParams(Draw draw) : this(draw.WKT, draw.DrawSettings, draw.Param, draw.Layername, draw.SystemId, draw.LinkedFields) { }
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="wkt">Геометрия в формате WKT</param>
        /// <param name="draw">Легендаризация объекта</param>
        /// <param name="param">Общие параметры</param>
        /// <param name="layername">Имя слоя</param>
        protected DrawParams(string wkt, string settings, string param, string layername, string systemid, LinkedDBFields linkedDBFields)
        {
            Geometry = Aspose.Gis.Geometries.Geometry.FromText(wkt);
            DrawSettings = JObject.Parse(settings);
            Param = JObject.Parse(param);
            LayerName = layername;
            SystemId = Convert.ToInt32(systemid);
            LinkedDBFields = linkedDBFields;
        }
        #endregion
    }
}