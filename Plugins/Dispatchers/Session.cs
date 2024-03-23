using Plugins.Entities;
using Plugins.Logging;
using Plugins.View;

using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;

using Newtonsoft.Json.Linq;

using Oracle.ManagedDataAccess.Client;
using System.Threading.Tasks;

namespace Plugins
{
    class Session
    {
        #region Private Fields

        readonly ILogger logger;
        readonly EntitiesFactory factory;
        readonly HatchPatternLoader patternLoader;
        readonly LayerDispatcher layerDispatcher;
        readonly IBlocksCreater blocksFactory;
        readonly OracleDbDispatcher connection;
        /// <summary>
        /// Рисуемый горизонт
        /// </summary>
        readonly string gorizont;
        readonly Database db;
        
        DrawInfoWindow window;

        #endregion

        #region Ctor

        public Session(OracleDbDispatcher disp, string selectedGorizont, ILogger log)
        {
            gorizont = selectedGorizont;
            connection = disp;
            logger = log;

            db = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database;

            layerDispatcher = new LayerDispatcher();
            blocksFactory = new BlocksFactory(db, logger);
            factory = new EntitiesFactory(blocksFactory, logger);
            patternLoader = new HatchPatternLoader();
        }

        #endregion

        #region Public Properties

        public OracleDataReader DrawDataReader => connection.GetDrawParams(gorizont);
        public uint PrimitivesCount => System.Convert.ToUInt32(connection.Count(gorizont));
        public DrawInfoWindow Window => window;

        #endregion

        #region Public Methods

        public IDictionary<string, string> LoadHatchPattern(JObject settings) => patternLoader.Load(settings);
        public void Add(Primitive primitive)
        {
            if (layerDispatcher.TryAdd(primitive.LayerName))
            {
                try
                {
                    var entity = factory.Create(primitive);
                    entity.AppendToDrawing(db);
                }
                catch (System.Exception ex)
                {
                    logger.Log(LogLevel.Error, "", ex);
                }
            }
            else
            {
                logger.LogWarning("Не удалось отрисовать объект {0}", primitive);
            }
        }
        public void Run()
        {
            var model = new DrawInfoViewModel(this, logger);
            window = new DrawInfoWindow() { DataContext = model };
            window.Closed += model.HandleOperationCancel;
            window.ShowDialog();
        }

        #endregion
    }
}