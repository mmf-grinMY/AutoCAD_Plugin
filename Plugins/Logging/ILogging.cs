using System;

namespace Plugins.Logging
{
    public interface ILogger
    {
        void Log(LogLevel level, string message, System.Exception exception = null);
    }
    public interface ILoggerProvider
    {
        ILogger CreateLogger(string categoryName);
    }
    public interface ILoggerFactory
    {
        ILogger CreateLogger<T>();
    }
    public sealed class LoggerFactory : ILoggerFactory
    {
        readonly ILoggerProvider provider;
        LoggerFactory(ILoggerProvider loggerProvider)
        {
            provider = loggerProvider;
        }
        public static ILoggerFactory Create(ILoggerProvider provider)
        {
            return new LoggerFactory(provider);
        }
        public ILogger CreateLogger<T>() => provider.CreateLogger(typeof(T).ToString());
    }
    public static class LoggerExtensions
    {
        public static void LogInformation(this ILogger logger, string message, params object[] args) =>
            logger.Log(LogLevel.Info, string.Format(message, args));
        public static void LogWarning(this ILogger logger, string message, params object[] args) =>
            logger.Log(LogLevel.Warn, string.Format(message, args));
        public static void LogError(this ILogger logger, string message, params object[] args) =>
            logger.Log(LogLevel.Error, string.Format(message, args));
    }
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3,
    }
}
