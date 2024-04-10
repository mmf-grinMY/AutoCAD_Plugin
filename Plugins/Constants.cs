using Plugins.Logging;

using System.IO;
using System;

using Autodesk.AutoCAD.ApplicationServices;

using Newtonsoft.Json.Linq;

namespace Plugins
{
    /// <summary>
    /// Хранилище общих констант
    /// </summary>
    class Constants
    {
        #region Private Fields

        /// <summary>
        /// Расположение файла с параметрами подключения
        /// </summary>
        static string dbConfigPath;
        /// <summary>
        /// Расположение сборки
        /// </summary>
        static string assemblyPath;
        /// <summary>
        /// Логер событий
        /// </summary>
        static ILogger logger;
        /// <summary>
        /// Количественный предел у очереди рисуемых объектов
        /// </summary>
        static int queueLimit;
        /// <summary>
        /// Время сна потока чтения в одной итерации
        /// </summary>
        static int readerSleepTime;

        #endregion

        #region Public Properties

        /// <inheritdoc cref="dbConfigPath"/>
        public static string DbConfigPath => dbConfigPath;
        /// <inheritdoc cref="assemblyPath"/>
        public static string AssemblyPath => assemblyPath;
        /// <inheritdoc cref="logger"/>
        public static ILogger Logger => logger;
        /// <inheritdoc cref="queueLimit"/>
        public static int QueueLimit => queueLimit;
        ///<inheritdoc cref="readerSleepTime"/> 
        public static int ReaderSleepTime => readerSleepTime;

        #endregion

        #region Public Methods

        /// <summary>
        /// Инициализация общих констант плагина
        /// </summary>
        public static void Initialize()
        {
            try
            {
                const string logFile = "main.log";

                assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(Commands)).Location);

                if (string.IsNullOrWhiteSpace(assemblyPath))
                    throw new ArgumentException(nameof(assemblyPath), "Не удалось получить расположение сборки плагина!");

                logger = new FileLogger(Path.Combine(assemblyPath, logFile));
                dbConfigPath = System.IO.Path.GetTempFileName();

                var config = JObject.Parse(File.ReadAllText(Path.Combine(assemblyPath, CONFIG_FILE)));
                
                var queueConfig = config.Value<JObject>("queue");
                queueLimit = queueConfig.Value<int>("limit");
                readerSleepTime = queueConfig.Value<int>("sleep");

                LoadFriendFolders();
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
        }
        /// <summary>
        /// Добавление пользовательских дружественных папок в AutoCAD
        /// </summary>
        public static void LoadFriendFolders()
        {
            dynamic app = Application.AcadApplication;

            string path = app.Preferences.Files.SupportPath;

            if (!path.Contains(assemblyPath))
            {
                // FIXME: !!! Может повредить дружественные папки AutoCAD !!!
                app.Preferences.Files.SupportPath = path + ";" + Path.Combine(assemblyPath, HATCHES) + ";" + assemblyPath;
            }
        }
        /// <summary>
        /// Удаление пользовательских дружественных папок в AutoCAD
        /// </summary>
        public static void UnloadFriendFolders()
        {
            dynamic app = Application.AcadApplication;

            string path = app.Preferences.Files.SupportPath;

            if (!path.Contains(assemblyPath))
            {
                app.Preferences.Files.SupportPath = 
                    path.Replace(Path.Combine(assemblyPath, HATCHES), string.Empty)
                        .Replace(assemblyPath, string.Empty);
            }
        }

        #endregion

        #region Public Const Fields

        /// <summary>
        /// Ключевое слово в XData для нахождения столбца SystemId
        /// </summary>
        public const string SYSTEM_ID = "varMM_SystemID";
        /// <summary>
        /// Ключевое слово в XData для нахождения столбца линковоной таблицы
        /// </summary>
        public const string BASE_NAME = "varMM_BaseName";
        /// <summary>
        /// Ключевое слово в XData для нахождения столбца линковки
        /// </summary>
        public const string LINK_FIELD = "varMM_LinkField";
        /// <summary>
        /// Id объекта в БД Oracle
        /// </summary>
        public const string OBJ_ID = "OBJ_ID";
        /// <summary>
        /// Главный конфигурационный файл
        /// </summary>
        public const string CONFIG_FILE = "plugin.config.json";
        /// <summary>
        /// Корневая папка паттернов штриховки
        /// </summary>
        public const string HATCHES = "Hatches";

        #endregion
    }
}