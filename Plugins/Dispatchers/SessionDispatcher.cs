using Plugins.Logging;

using System.IO;

namespace Plugins
{
    /// <summary>
    /// Диспетчер сессий работы
    /// </summary>
    static class SessionDispatcher
    {
        /// <summary>
        /// Фабрика логеров
        /// </summary>
        public static ILoggerFactory factory;
        /// <summary>
        /// Текущая сессия
        /// </summary>
        static Session session;
        /// <summary>
        /// Текущий логер сессии
        /// </summary>
        static ILogger logger;
        /// <summary>
        /// Инициализация класса
        /// </summary>
        static SessionDispatcher()
        {
            factory = LoggerFactory.Create(new FileLoggerProvider(Path.Combine(Constants.AssemblyPath, "main.log")));
        }
        /// <summary>
        /// Текущий логер сессии
        /// </summary>
        public static ILogger Logger
        {
            get
            {
                if (logger is null)
                    logger = factory.CreateLogger();

                return logger;
            }
        }
        /// <summary>
        /// Запустить сессию работы
        /// </summary>
        /// <param name="disp">Диспетчер подключения к БД</param>
        /// <param name="gor">Текущий горизонт</param>
        public static void StartSession(OracleDbDispatcher disp, string gor)
        {
            logger = factory.CreateLogger();
            session = new Session(disp, gor, logger);
            session.Run();
        }
        /// <summary>
        /// Текщуая сессия работы
        /// </summary>
        public static Session Current => session;
    }
}