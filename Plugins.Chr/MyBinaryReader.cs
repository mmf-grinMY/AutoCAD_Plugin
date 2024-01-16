using System;
using System.IO;

namespace Plugins.Chr
{
    /// <summary>
    /// Читатель бинарных файлов
    /// </summary>
    class MyBinaryReader : BinaryReader
    {
        #region Public Properties
        /// <summary>
        /// Текущая позиция в файле
        /// </summary>
        public ulong CurrentPosition { private set; get; }
        /// <summary>
        /// Стартовая позиция чтения
        /// </summary>
        public ulong StartPosition { private set; get; }
        #endregion

        #region Ctors
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="path">Местоположение читаемого файла</param>
        public MyBinaryReader(string path) : base(File.OpenRead(path))
        {
            StartPosition = CurrentPosition = 0;
            var parts = path.Replace('\\', '/').Split('/');
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Прочитать байт из потока
        /// </summary>
        /// <returns>Прочитанный байт</returns>
        public override byte ReadByte()
        {
            CurrentPosition++;
            return base.ReadByte();
        }
        /// <summary>
        /// Прочитать несколько байтов
        /// </summary>
        /// <param name="count">Количество читаемых байтов/param>
        /// <returns>Массив прочитанных байтов</returns>
        /// <exception cref="ArgumentOutOfRangeException">Вывзывается, если количество байтов меньше нуля</exception>
        public override byte[] ReadBytes(int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException("count must be greather than 0");
            CurrentPosition += (ulong)count;
            return base.ReadBytes(count);
        }
        /// <summary>
        /// Перейти к позиции в файле
        /// </summary>
        /// <param name="position">Позиция для перехода</param>
        /// <returns>true, если перемещение удалось и false в противном случае</returns>
        public bool Seek(ulong position)
        {
            if (position == CurrentPosition - 1)
            {
                
            }
            else if (position > CurrentPosition - 1)
            {
                _ = base.ReadBytes((int)(position - CurrentPosition));
                CurrentPosition = position;
            }
            else
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Пропустить несколько символов
        /// </summary>
        /// <param name="length">Количество символов, которые стоит пропустить</param>
        public void Skip(int length = 1)
        {
            _ = ReadBytes(length);
        }
        #endregion
    }
}