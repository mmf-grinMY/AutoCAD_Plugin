// #define FAST_DEBUG

using Plugins.Dispatchers;
using Plugins.Entities;
using Plugins.Logging;
using Plugins.View;

using System;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Oracle.ManagedDataAccess.Client;

using static Plugins.Constants;

namespace Plugins
{
    /// <summary>
    /// Сеанс работы плагина
    /// </summary>
    class Session : IDisposable
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

        #region Private Methods

        /// <summary>
        /// Получить запись из таблицы RegAppTable
        /// </summary>
        /// <param name="db">Текущая БД документа AutoCAD</param>
        /// <param name="logger">Логер событий</param>
        /// <returns>Действие получения записи из таблицы RegAppTable</returns>
        /// <exception cref="InvalidOperationException"></exception>
        Action<string> GetRegAppAction(Database db, ILogger logger)
        {
            var regAppTableDispatcher = new RegAppTableDispatcher(db, logger);

            return (string name) =>
            {
                if (!regAppTableDispatcher.TryAdd(name))
                    throw new InvalidOperationException("Не удалось сохранить RegApp " + name + "!");
            };
        }

        #endregion

        #region Ctor

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="logger">Логер событий</param>
        public Session(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            connection = new OracleDbDispatcher(
#if FAST_DEBUG
            "Data Source=data-pc/GEO;Password=g1;User Id=g;Connection Timeout=360;", "K305F"
#endif
            );
            doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument
                ?? throw new ArgumentNullException(nameof(doc));
            db = doc.Database ?? throw new ArgumentNullException(nameof(db));

            layerDispatcher = new LayerTableDispatcher(db, this.logger);
            blocksFactory = new BlockTableDispatcher(db, this.logger);
            factory = new EntitiesFactory(blocksFactory, this.logger, connection);

            var addRegApp = GetRegAppAction(db, logger);

            addRegApp(SYSTEM_ID);
            addRegApp(BASE_NAME);
            addRegApp(LINK_FIELD);
            addRegApp(OBJ_ID);

            // TODO: Вынести в конфигурационный файл
            const string LINE_TYPE_SOURCE = "acad.lin";
            const string TYPE_NAME = "MMP_2";

            try
            {
                db.LoadLineTypeFile(TYPE_NAME, LINE_TYPE_SOURCE);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception)
            {
                logger.LogError("Не удалось найти стиль линии \"" + TYPE_NAME + "\" в файле \"" + LINE_TYPE_SOURCE + "\"!");
                throw;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Количество примитивов на горизонте, доступных для отрисовки
        /// </summary>
        public uint PrimitivesCount => System.Convert.ToUInt32(connection.Count);
        public long Left { get; set; }
        public long Right { get; set; }
        public long Top { get; set; }
        public long Bottom { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Освобождение неуправляемых ресурсов
        /// </summary>
        public void Dispose()
        {
            connection.Dispose();
        }
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
                catch (Exception e)
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
            var model = new DrawInfoViewModel(this, logger, connection);
            window = new DrawInfoWindow() { DataContext = model };
            window.Closed += model.HandleOperationCancel;
            if (!isClosed)
                window.ShowDialog();

            try
            {
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor
                    .Zoom(new Extents3d(new Point3d(Left, Bottom, 0), new Point3d(Right, Top, 0)));
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("\n" + "Wrong BB");
            }
        }
        bool isClosed = false;
        /// <summary>
        /// Закрытие сессии работы
        /// </summary>
        public void Close()
        {
            if (window != null)
                window.Dispatcher.Invoke(() => window.Close());
            isClosed = true;
        }
        public void WriteMessage(string message)
        {
            if (window != null)
                window.Dispatcher.Invoke(() => doc.Editor.WriteMessage(message));
        }

        #endregion
    }
}