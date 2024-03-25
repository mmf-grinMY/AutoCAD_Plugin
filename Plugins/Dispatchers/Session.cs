using Plugins.Entities;
using Plugins.Logging;
using Plugins.View;

using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;

using Newtonsoft.Json.Linq;

using Oracle.ManagedDataAccess.Client;

namespace Plugins
{
    /// <summary>
    /// Сеанс работы плагина
    /// </summary>
    class Session
    {
        #region Private Fields

        /// <summary>
        /// Логер событий
        /// </summary>
        readonly ILogger logger;
        /// <summary>
        /// Создатель примитивов для отрисовки
        /// </summary>
        readonly EntitiesFactory factory;
        /// <summary>
        /// Загрузчик штриховок
        /// </summary>
        readonly HatchPatternLoader patternLoader;
        /// <summary>
        /// Диспетчер слоев AutoCAD
        /// </summary>
        readonly LayerDispatcher layerDispatcher;
        /// <summary>
        /// Создатель блоков AutoCAD
        /// </summary>
        readonly IBlocksCreater blocksFactory;
        /// <summary>
        /// Диспетчер подключения к БД Oracle
        /// </summary>
        readonly OracleDbDispatcher connection;
        /// <summary>
        /// Рисуемый горизонт
        /// </summary>
        readonly string gorizont;
        /// <summary>
        /// Внутренняя БД AutoCAD
        /// </summary>
        readonly Database db;
        /// <summary>
        /// Монитор прогресса отрисовки
        /// </summary>
        DrawInfoWindow window;

        #endregion

        #region Ctor

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="disp">Диспетчер БД Oracle</param>
        /// <param name="selectedGorizont">Выбранный для отрисовки горизонт</param>
        /// <param name="log">Логер событий</param>
        public Session(OracleDbDispatcher disp, string selectedGorizont, ILogger log)
        {
            gorizont = selectedGorizont;
            connection = disp;
            logger = log;

            db = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database;

            layerDispatcher = new LayerDispatcher(db, logger);
            blocksFactory = new BlocksFactory(db, logger);
            factory = new EntitiesFactory(blocksFactory, logger);
            patternLoader = new HatchPatternLoader();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Читатель объектов для отрисовки
        /// </summary>
        public OracleDataReader DrawDataReader => connection.GetDrawParams(gorizont);
        /// <summary>
        /// Количество примитивов на горизонте, доступных для отрисовки
        /// </summary>
        public uint PrimitivesCount => System.Convert.ToUInt32(connection.Count(gorizont));
        /// <summary>
        /// Монитор прогресса отрисовки
        /// </summary>
        public DrawInfoWindow Window => window;

        #endregion

        #region Public Methods

        /// <summary>
        /// Загрузить паттерн штриховки
        /// </summary>
        /// <param name="settings">Настройки штриховки</param>
        /// <returns>Словарь параметров штриховки</returns>
        public IDictionary<string, string> LoadHatchPattern(JObject settings) => patternLoader.Load(settings);
        /// <summary>
        /// Нарисовать примитив
        /// </summary>
        /// <param name="primitive">Объект отрисовки</param>
        public void Add(Primitive primitive)
        {
            if (layerDispatcher.TryAdd(primitive.LayerName))
            {
                try
                {
                    var entity = factory.Create(primitive);
                    entity.AppendToDrawing(db);
                }
                catch (System.Exception e)
                {
                    logger.LogError(e);
                }
            }
            else
            {
                logger.LogWarning("Не удалось отрисовать объект {0}", primitive);
            }
        }
        /// <summary>
        /// Запустить процесс отрисовки
        /// </summary>
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