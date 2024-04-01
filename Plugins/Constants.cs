using Plugins.Logging;

using System.IO;
using System;

using Autodesk.AutoCAD.ApplicationServices;

using Newtonsoft.Json.Linq;

namespace Plugins
{
    // TODO: Добавить константу масштабирования для блоков
    /// <summary>
    /// Хранилище общих констант
    /// </summary>
    class Constants
    {
        /// <summary>
        /// Расположение файла с параметрами подключения
        /// </summary>
        public static string dbConfigPath;
        /// <summary>
        /// Расположение сборки
        /// </summary>
        static string assemblyPath;
        static ILogger logger;
        public static ILogger Logger => logger;
        public static string AssemblyPath => assemblyPath;

        public const string CONFIG_FILE = "plugin.config.json";
        public string DbConfigPath => dbConfigPath;
        static int queueLimit;
        static int readerSleepTime;
        public static int QueueLimit => queueLimit;
        public static int ReaderSleepTime => readerSleepTime;
        static double textScale;
        static double hatchScale;
        public static void Initialize()
        {
            dynamic app = Application.AcadApplication;

            try
            {
                const string logFile = "main.log";

                assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(Commands)).Location);

                if (string.IsNullOrWhiteSpace(assemblyPath))
                    throw new ArgumentException(nameof(assemblyPath), "Не удалось получить расположение сборки плагина!");

                logger = new FileLogger(Path.Combine(assemblyPath, logFile));
                dbConfigPath = System.IO.Path.GetTempFileName();

                var config = JObject.Parse(File.ReadAllText(Path.Combine(assemblyPath, CONFIG_FILE)));
                textScale = config.Value<double>("TextScale");
                hatchScale = config.Value<double>("HatchScale");
                config = config.Value<JObject>("queue");
                queueLimit = config.Value<int>("limit");
                readerSleepTime = config.Value<int>("sleep");

                string path = app.Preferences.Files.SupportPath;

                if (!path.Contains(assemblyPath))
                {
                    // FIXME: !!! Может повредить дружественные папки AutoCAD !!!
                    app.Preferences.Files.SupportPath = path + ";" + assemblyPath;
                }
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
        }

        #region Константы масштабирования

        /// <summary>
        /// Масштаб рисуемого текста относительно общего масштаба примитивов
        /// </summary>
        public static double TextScale => textScale;
        /// <summary>
        /// Масштаб штриховки относительно общего масштаба примитивов
        /// </summary>
        public static double HatchScale => hatchScale;

        #endregion

        #region Ключевые слова для XData

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

        #endregion
    }
}