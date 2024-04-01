using Plugins.Dispatchers;
using Plugins.Entities;
using Plugins.Logging;
using Plugins.View;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

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
        /// Диспетчер слоев AutoCAD
        /// </summary>
        readonly SymbolTableDispatcher layerDispatcher;
        /// <summary>
        /// Создатель блоков AutoCAD
        /// </summary>
        readonly SymbolTableDispatcher blocksFactory;
        /// <summary>
        /// Диспетчер подключения к БД Oracle
        /// </summary>
        readonly OracleDbDispatcher connection;
        /// <summary>
        /// Внутренняя БД AutoCAD
        /// </summary>
        readonly Database db;
        /// <summary>
        /// Текущий документ AutoCAD
        /// </summary>
        readonly Document doc;
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
        public Session(OracleDbDispatcher disp, ILogger log)
        {
            connection = disp;
            logger = log;

            doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            db = doc.Database;

            layerDispatcher = new LayerTableDispatcher(db, logger);
            blocksFactory = new BlockTableDispatcher(db, logger);
            factory = new EntitiesFactory(blocksFactory, logger);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Количество примитивов на горизонте, доступных для отрисовки
        /// </summary>
        public uint PrimitivesCount => System.Convert.ToUInt32(connection.Count(gorizont));

        #endregion

        #region Public Methods

        /// <summary>
        /// Читатель объектов для отрисовки
        /// </summary>
        public OracleDataReader DrawDataReader(uint position) => connection.GetDrawParams(gorizont, position);
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
        /// <summary>
        /// Закрытие сессии работы
        /// </summary>
        public void Close() => window.Dispatcher.Invoke(() => window.Close());
        public void WriteMessage(string message) => window.Dispatcher.Invoke(() => doc.Editor.WriteMessage(message));

        #endregion
    }
}