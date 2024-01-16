using System;

namespace Plugins.Chr
{
    /// <summary>
    /// Неправильный формат файла
    /// </summary>
    public class BadFileFormatException : Exception
    {
        /// <summary>
        /// Создание объекта класса
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        public BadFileFormatException(string message) : base(message) { }
    }
}
