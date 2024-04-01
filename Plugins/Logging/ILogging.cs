namespace Plugins.Logging
{
    /// <summary>
    /// Логер событий
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Логировать событие
        /// </summary>
        /// <param name="level">Уровень логирования</param>
        /// <param name="message">Сообщение логов</param>
        /// <param name="ex">Сопутствующее исключение</param>
        void Log(LogLevel level, string message = null, System.Exception exception = null);
    }
    /// <summary>
    /// Расширения логеров
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Записать общую информацию
        /// </summary>
        /// <param name="logger">Текущий логер</param>
        /// <param name="message">Записываемое сообщение</param>
        /// <param name="args">Другие аргументы</param>
        public static void LogInformation(this ILogger logger, string message, params object[] args) =>
            logger.Log(LogLevel.Info, string.Format(message, args));
        /// <summary>
        /// Записать предупреждение
        /// </summary>
        /// <param name="logger">Текущий логер</param>
        /// <param name="message">Записываемое сообщение</param>
        /// <param name="args">Другие аргументы</param>
        public static void LogWarning(this ILogger logger, string message, params object[] args) =>
            logger.Log(LogLevel.Warn, string.Format(message, args));
        /// <summary>
        /// Записать ошибку исполнения
        /// </summary>
        /// <param name="logger">Текущий логер</param>
        /// <param name="message">Записываемое сообщение</param>
        /// <param name="args">Другие аргументы</param>
        public static void LogError(this ILogger logger, string message, params object[] args) =>
            logger.Log(LogLevel.Error, string.Format(message, args));
        /// <summary>
        /// Записать ошибку исполнения
        /// </summary>
        /// <param name="logger">Текущий логер</param>
        /// <param name="ex">Полученное в результате работы исключение</param>
        public static void LogError(this ILogger logger, System.Exception ex) =>
            logger.Log(LogLevel.Error, exception: ex);
    }
    /// <summary>
    /// Уровень логирования
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3,
    }
}
