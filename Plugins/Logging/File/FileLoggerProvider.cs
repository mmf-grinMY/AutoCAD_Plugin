using System.IO;

namespace Plugins.Logging
{
    /// <summary>
    /// Провайдер файлового логера
    /// </summary>
    sealed class FileLoggerProvider : ILoggerProvider
    {
        /// <summary>
        /// Расположение файла логов
        /// </summary>
        readonly string filePath;
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="path">Расположение файла логов</param>
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