﻿using System;

using Aspose.Gis.Geometries;
using Newtonsoft.Json.Linq;

namespace Plugins
{
    /// <summary>
    /// Параметры отрисовки
    /// </summary>
    public class DrawParams
    {
        #region Private Fields
        private string layername;
        #endregion

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
        public string LayerName 
        {
            get => layername;
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException(nameof(value));

                layername = value;
            }
        }
        #endregion

        #region Ctors
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="draw">Строковые параметры отрисовки</param>
        public DrawParams(Draw draw) : this(draw.WKT, draw.DrawSettings, draw.Param, draw.Layername) { }
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="wkt">Геометрия в формате WKT</param>
        /// <param name="draw">Легендаризация объекта</param>
        /// <param name="param">Общие параметры</param>
        /// <param name="layername">Имя слоя</param>
        protected DrawParams(string wkt, string settings, string param, string layername)
        {
            Geometry = Aspose.Gis.Geometries.Geometry.FromText(wkt);
            DrawSettings = JObject.Parse(settings);
            Param = JObject.Parse(param);
            LayerName = layername;
        }
        #endregion
    }
}