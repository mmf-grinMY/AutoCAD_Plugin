using System;

using Newtonsoft.Json.Linq;

namespace Plugins.Entities
{
    /// <summary>
    /// Параметры отрисовки
    /// </summary>
    public class Primitive
    {
        #region Public Properties

        /// <summary>
        /// Геометрический объект
        /// </summary>
        public string Geometry { get; }
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
        /// Имя слинкованной таблицы
        /// </summary>
        public string BaseName { get; }
        /// <summary>
        /// Столбец линковки
        /// </summary>
        public string ChildField { get; }

        #endregion

        #region Ctors
        
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="wkt">Геометрия примитива в формате WKT</param>
        /// <param name="settings">Параметры отрисовки</param>
        /// <param name="param">Дополнительные параметры</param>
        /// <param name="layername">Имя слоя</param>
        /// <param name="systemid">Уникальный номер</param>
        /// <param name="baseName">Имя слинкованной таблицы</param>
        /// <param name="childFields">Столбец линковки</param>
        public Primitive(string wkt, string settings, string param, string layername, string systemid, string baseName, string childFields)
        {
            Geometry = wkt;
            DrawSettings = JObject.Parse(settings);
            Param = JObject.Parse(param);
            LayerName = layername;
            SystemId = Convert.ToInt32(systemid);
            BaseName = baseName;
            ChildField = childFields;
        }

        #endregion
    }
}