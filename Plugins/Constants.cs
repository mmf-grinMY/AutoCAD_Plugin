using System.IO;

using Autodesk.AutoCAD.ApplicationServices;

using Newtonsoft.Json.Linq;

//TODO: Проверить корректность инициализации констант
namespace Plugins
{
    /// <summary>
    /// Хранилище общих констант
    /// </summary>
    class Constants
    {
        /// <summary>
        /// Расположение файла с параметрами подключения
        /// </summary>
        static readonly string DB_CONFIG_PATH;
        /// <summary>
        /// Расположение папки AutoCAD Support
        /// </summary>
        static readonly string SUPPORT_PATH;
        /// <summary>
        /// Расположение сборки
        /// </summary>
        static readonly string ASSEMBLY_PATH;

        public readonly static string CONFIG_FILE;
        public static readonly int QUEUE_LIMIT;
        public static readonly int READER_SLEEP_TIME;
        static Constants()
        {
            CONFIG_FILE = "plugin.config.json";

            var fileName = Application.DocumentManager.MdiActiveDocument.Database.Filename;
            SUPPORT_PATH = Path.Combine(Directory.GetParent(
                    Path.GetDirectoryName(fileName)).FullName, "Support").Replace("Local", "Roaming");
            DB_CONFIG_PATH = System.IO.Path.GetTempFileName();
            ASSEMBLY_PATH = Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(Commands)).Location);

            var config = JObject.Parse(File.ReadAllText(Path.Combine(Constants.SupportPath, CONFIG_FILE)));

            SCALE = config.Value<double>("Scale");
            TEXT_SCALE = config.Value<double>("TextScale") * SCALE;
            HATCH_SCALE = config.Value<double>("HatchScale") * SCALE * SCALE;

            config = config.Value<JObject>("queue");

            QUEUE_LIMIT = config.Value<int>("limit");
            READER_SLEEP_TIME = config.Value<int>("sleep");
        }

        #region Константы масштабирования

        /// <summary>
        /// Масштаб рисуемых примитивов
        /// </summary>
        public static readonly double SCALE;
        /// <summary>
        /// Масштаб рисуемого текста относительно общего масштаба примитивов
        /// </summary>
        public static readonly double TEXT_SCALE;
        /// <summary>
        /// Масштаб штриховки относительно общего масштаба примитивов
        /// </summary>
        public static readonly double HATCH_SCALE;

        #endregion

        #region Ключевые слова для XData

        /// <summary>
        /// Ключевое слово в XData для нахождения столбца SystemId
        /// </summary>
        public static readonly string SYSTEM_ID = "varMM_SystemID";
        /// <summary>
        /// Ключевое слово в XData для нахождения столбца линковоной таблицы
        /// </summary>
        public static readonly string BASE_NAME = "varMM_BaseName";
        /// <summary>
        /// Ключевое слово в XData для нахождения столбца линковки
        /// </summary>
        public static readonly string LINK_FIELD = "varMM_LinkField";

        #endregion

        #region Public Properties

        /// <summary>
        /// Расположение файла с параметрами подключения
        /// </summary>
        public static string DbConfigFilePath => DB_CONFIG_PATH;
        /// <summary>
        /// Расположение папки AutoCAD Support
        /// </summary>
        public static string SupportPath => SUPPORT_PATH;
        public static string AssemblyPath => ASSEMBLY_PATH;

        #endregion
    }
}