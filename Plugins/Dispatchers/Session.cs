using Plugins.Dispatchers;
using Plugins.Entities;
using Plugins.Logging;
using Plugins.View;

using System.IO;
using System;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Newtonsoft.Json.Linq;

using static Plugins.Constants;

namespace Plugins
{
    /// <summary>
    /// Сеанс работы команды отрисовки
    /// </summary>
    class Session : IDisposable
    {
        #region Private Readonly Fields

        /// <summary>
        /// Логер событий
        /// </summary>
        readonly ILogger logger;
        /// <summary>
        /// Создатель примитивов для отрисовки
        /// </summary>
        readonly EntitiesFactory factory;
        /// <summary>
        /// Создатель блоков AutoCAD
        /// </summary>
        readonly ITableDispatcher blockDispatcher;
        /// <summary>
        /// Диспетчер для работы с таблицей RegAppTable
        /// </summary>
        readonly ITableDispatcher regAppDispatcher;
        /// <summary>
        /// Диспетчер подключения к БД Oracle
        /// </summary>
        readonly IDbDispatcher connection;
        /// <summary>
        /// Внутренняя БД AutoCAD
        /// </summary>
        readonly Database db;
        /// <summary>
        /// Текущий документ AutoCAD
        /// </summary>
        readonly Document doc;

        #endregion

        #region Private Fields

        /// <summary>
        /// Окно управления процессом отрисовки
        /// </summary>
        DrawInfoWindow window;
        /// <summary>
        /// Статус закрытия сессии
        /// </summary>
        bool isClosed = false;

        #endregion

        #region Private Methods

        /// <summary>
        /// Добавить запись в RegAppTable
        /// </summary>
        /// <param name="name">Регистрируемое имя</param>
        /// <exception cref="InvalidOperationException"></exception>
        void RegApp(string name)
        {
            if (!regAppDispatcher.TryAdd(name))
                throw new InvalidOperationException("Не удалось сохранить RegApp " + name + "!");
        }

        #endregion

        #region Ctor

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="logger">Логер событий</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public Session(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            connection = new OracleDbDispatcher();
            doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument
                ?? throw new ArgumentNullException(nameof(doc));
            db = doc.Database ?? throw new ArgumentNullException(nameof(db));

            regAppDispatcher = new RegAppTableDispatcher(db, this.logger);
            blockDispatcher = new BlockTableDispatcher(db, this.logger);
            factory = new EntitiesFactory(blockDispatcher, connection);

            RegApp(SYSTEM_ID);
            RegApp(BASE_NAME);
            RegApp(LINK_FIELD);
            RegApp(OBJ_ID);

            var config = JObject.Parse(File.ReadAllText(Path.Combine(AssemblyPath, CONFIG_FILE))).Value<JObject>("linetype");

            var source = config.Value<string>("source");
            foreach (var name in config.Value<JArray>("linetypes"))
            {
                try
                {
                    db.LoadLineTypeFile(name.Value<string>(), Path.Combine(AssemblyPath, source));
                }
                catch (Autodesk.AutoCAD.Runtime.Exception)
                {
                    logger.LogError("Не удалось найти стиль линии \"{0}}\" в файле \"{1}\"!", name, source);
                    throw;
                }
            }

            var layerDispatcher = new LayerTableDispatcher(db, this.logger);

            foreach (var layer in connection.GetLayers())
            {
                if (!layerDispatcher.TryAdd(layer)) logger.LogInformation("Не удалось создать слой " + layer + "!");
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Количество примитивов на горизонте, доступных для отрисовки
        /// </summary>
        public uint PrimitivesCount => System.Convert.ToUInt32(connection.Count);
        /// <summary>
        /// Координата по оси X левой крайней точки
        /// </summary>
        public long Left { get; set; }
        /// <summary>
        /// Координата по оси X правой крайней точки
        /// </summary>
        public long Right { get; set; }
        /// <summary>
        /// Координата по оси Y верхней крайней точки
        /// </summary>
        public long Top { get; set; }
        /// <summary>
        /// Координата по оси Y нижней крайней точки
        /// </summary>
        public long Bottom { get; set; }
        /// <summary>
        /// Учет граничных точек
        /// </summary>
        public bool IsBoundingBoxChecked => connection.IsBoundingBoxChecked;

        #endregion

        #region Public Methods

        /// <summary>
        /// Освобождение неуправляемых ресурсов
        /// </summary>
        public void Dispose() => connection?.Dispose();
        /// <summary>
        /// Нарисовать примитив
        /// </summary>
        /// <param name="primitive">Примитив отрисовки</param>
        public void Add(Primitive primitive)
        {
            if (primitive is null)
                throw new ArgumentNullException(nameof(primitive));

            try
            {
                factory.Create(primitive).AppendToDrawing(db, logger);
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
        }
        /// <summary>
        /// Запустить процесс отрисовки
        /// </summary>
        public void Run()
        {
            try
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

                logger.LogInformation("Закончена отрисовка геометрии!");
            }
            catch (ArgumentOutOfRangeException) { }
        }
        /// <summary>
        /// Закрытие сессии работы
        /// </summary>
        public void Close()
        {
            if (window != null)
                window.Dispatcher.Invoke(() => window.Close());
            isClosed = true;
        }
        /// <summary>
        /// Записать сообщение в командную строку AutoCAD
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        public void WriteMessage(string message)
        {
            if (window != null)
                window.Dispatcher.Invoke(() => doc.Editor.WriteMessage(message));
        }

        #endregion
    }
}