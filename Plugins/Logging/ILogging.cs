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
    /// Провайдер логера
    /// </summary>
    public interface ILoggerProvider
    {
        /// <summary>
        /// Создать логер
        /// </summary>
        /// <param name="categoryName">Имя категории логера</param>
        /// <returns>Новый логер</returns>
        ILogger CreateLogger(string categoryName);
    }
    /// <summary>
    /// Фабрика логеров
    /// </summary>
    public interface ILoggerFactory
    {
        /// <summary>
        /// Создать новый логер
        /// </summary>
        /// <typeparam name="T">Тип владельца логера</typeparam>
        /// <returns>Новый логер</returns>
        ILogger CreateLogger<T>();
        /// <summary>
        /// Создать новый логер
        /// </summary>
        /// <returns>Новый логер</returns>
        ILogger CreateLogger();
    }
    /// <summary>
    /// Фабрика логеров
    /// </summary>
    public sealed class LoggerFactory : ILoggerFactory
    {
        /// <summary>
        /// Провайдер логеров
        /// </summary>
        readonly ILoggerProvider provider;
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="loggerProvider">Провайдер логеров</param>
        LoggerFactory(ILoggerProvider loggerProvider)
        {
            provider = loggerProvider;
        }
        /// <summary>
        /// Создать новую фабрику
        /// </summary>
        /// <param name="provider">Провайдер логеров</param>
        /// <returns>Новая фабрика логеров</returns>
        public static ILoggerFactory Create(ILoggerProvider provider)
        {
            return new LoggerFactory(provider);
        }
        public ILogger CreateLogger<T>() => provider.CreateLogger(typeof(T).ToString());
        public ILogger CreateLogger() => provider.CreateLogger(string.Empty);
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
