using System.IO;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

using Newtonsoft.Json.Linq;

namespace Plugins
{
    /// <summary>
    /// Хранилище общих констант
    /// </summary>
    class Constants
    {
        static Constants()
        {
            var fileName = Application.DocumentManager.MdiActiveDocument.Database.Filename;
            supportPath = Path.Combine(Directory.GetParent(
                    Path.GetDirectoryName(fileName)).FullName, "Support").Replace("Local", "Roaming");
            dbConfigFilePath = System.IO.Path.GetTempFileName();

            var config = JObject.Parse(File.ReadAllText(Path.Combine(Constants.SupportPath, "plugin.config.json")));

            SCALE = config.Value<double>("Scale");
            TEXT_SCALE = config.Value<double>("TextScale") * SCALE;
            HATCH_SCALE = config.Value<double>("HatchScale") * SCALE * SCALE;
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

        #region Расположение вспомогательных папок и файлов
        /// <summary>
        /// Расположение файла с параметрами подключения
        /// </summary>
        private static string dbConfigFilePath;
        /// <summary>
        /// Расположение папки AutoCAD Support
        /// </summary>
        private static string supportPath;
        #endregion

        #region Public Properties
        /// <summary>
        /// Расположение файла с параметрами подключения
        /// </summary>
        public static string DbConfigFilePath => dbConfigFilePath;
        /// <summary>
        /// Расположение папки AutoCAD Support
        /// </summary>
        public static string SupportPath => supportPath;
        #endregion

        // public static ViewTableRecord OldView { get; set; }
        public static double OldWidth { get; set; }
    }
}