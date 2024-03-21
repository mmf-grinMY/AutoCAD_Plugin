using Plugins.Entities;

using System.Collections.Generic;
using System.Windows;

using Newtonsoft.Json.Linq;

using Oracle.ManagedDataAccess.Client;

namespace Plugins
{
    class Session
    {
        #region Private Fields

        readonly EntitiesFactory factory;
        readonly HatchPatternLoader patternLoader;
        readonly LayerDispatcher layerDispatcher;
        readonly BlocksFactory blocksFactory;
        readonly OracleDbDispatcher connection;
        /// <summary>
        /// Рисуемый горизонт
        /// </summary>
        readonly string gorizont;

        #endregion

        #region Ctor

        public Session(OracleDbDispatcher disp, string selectedGorizont)
        {
            gorizont = selectedGorizont;
            connection = disp;

            layerDispatcher = new LayerDispatcher();
            blocksFactory = new BlocksFactory();
            factory = new EntitiesFactory();
            patternLoader = new HatchPatternLoader();
        }

        #endregion

        #region Public Properties

        public OracleDataReader DrawDataReader => connection.GetDrawParams(gorizont);
        public uint PrimitivesCount => System.Convert.ToUInt32(connection.Count(gorizont));

        #endregion

        #region Public Methods

        public bool Create(string key) => blocksFactory.Create(key);
        public IDictionary<string, string> LoadHatchPattern(JObject settings) => patternLoader.Load(settings);
        public bool TryAdd(Primitive primitive)
        {
            if (layerDispatcher.TryAdd(primitive.LayerName))
            {
                try
                {
                    var entity = factory.Create(primitive);
                    if (entity != null)
                    {
                        entity.Draw();
                        entity.Dispose();

                        return true;
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            return false;
        }

        #endregion
    }
}