using Plugins.Logging;

using System.IO;

namespace Plugins
{
    static class SessionDispatcher
    {
        public static ILoggerFactory factory;
        static Session session;
        static ILogger logger;
        static SessionDispatcher()
        {
            var path = Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(Commands)).Location);
            factory = LoggerFactory.Create(new FileLoggerProvider(Path.Combine(path, "main.log")));
        }
        public static ILogger Logger
        {
            get
            {
                if (logger is null)
                    logger = factory.CreateLogger();

                return logger;
            }
        }
        public static void StartSession(OracleDbDispatcher disp, string gor)
        {
            logger = factory.CreateLogger();
            session = new Session(disp, gor, logger);
            session.Run();
        }
        public static Session Current => session;
    }
}