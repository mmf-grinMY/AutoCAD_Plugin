namespace Plugins
{
    /// <summary>
    /// Хранилище общих констант
    /// </summary>
    class Constants
    {
        /// <summary>
        /// Масштаб отрисовываемых объектов
        /// </summary>
        public static readonly int SCALE = 1_000;
        public static readonly string SYSTEM_ID = "varMM_SystemID";
        public static readonly string BASE_NAME = "varMM_BaseName";
        public static readonly string LINK_FIELD = "varMM_LinkField";
        private static string supportPath;
        public static string SupportPath => supportPath;
        public static void SetSupportPath(string path)
        {
            supportPath = path;
        }
    }
}