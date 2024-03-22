using System.IO;

namespace Plugins.Logging
{
    sealed class FileLoggerProvider : ILoggerProvider
    {
        readonly string filePath;
        public FileLoggerProvider(string path)
        {
            filePath = path;
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(filePath);
        }
    }
}