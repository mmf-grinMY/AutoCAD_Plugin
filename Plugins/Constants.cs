namespace Plugins
{
    /// <summary>
    /// Хранилище общих констант
    /// </summary>
    class Constants
    {
        #region Константы масштабирования
        /// <summary>
        /// Масштаб рисуемых примитивов
        /// </summary>
        public static readonly int SCALE = 1_000;
        /// <summary>
        /// Масштаб рисуемого текста относительно общего масштаба примитивов
        /// </summary>
        public static readonly double TEXT_SCALE = 0.5 * SCALE;
        /// <summary>
        /// Масштаб штриховки относительно общего масштаба примитивов
        /// </summary>
        public static readonly double HATCH_SCALE = 0.8 * SCALE * SCALE;
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
        public static string SupportPath
        {
            get => supportPath;
            set
            {
                supportPath = value;
                dbConfigFilePath = System.IO.Path.GetTempFileName();
                // TODO: Добавить загрузку констант масштабирования из конфигурационного файла
            }
        }
        #endregion
    }
}