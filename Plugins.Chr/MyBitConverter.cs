using System;

namespace Plugins.Chr
{
    /// <summary>
    /// Конвертер типов данных
    /// </summary>
    public static class MyBitConverter
    {
        /// <summary>
        /// Конвертирует массив байтов в ushort
        /// </summary>
        /// <param name="bytes">Массив для конвертации</param>
        /// <returns>Сконвертированное число</returns>
        /// <exception cref="ArgumentException">Вызывается, если длина массива превышает размер ushort</exception>
        public static ushort ToUInt16(byte[] bytes)
        {
            if (bytes.Length > 2)
                throw new ArgumentException("Length of bytes must be 1 or 2");

            return (ushort)(bytes[1] << 8 | bytes[0]);
        }
        /// <summary>
        /// Устанавливает значение бита в 1 по номеру
        /// </summary>
        /// <param name="value">Исходный байт</param>
        /// <param name="bitNumber">Номер бита</param>
        /// <returns>Байт после установки бита</returns>
        public static sbyte Bit(byte value, int bitNumber)
        {
            return Convert.ToSByte((value & (1 << bitNumber)) >> bitNumber);
        }
        /// <summary>
        /// Обнуляет значение бита по номеру
        /// </summary>
        /// <param name="value">Исходный байт</param>
        /// <param name="bitNumber">номер бита</param>
        /// <returns>Байт после обнуления бита</returns>
        public static byte BitReset(byte value, int bitNumber)
        {
            return Convert.ToByte(value & (~(1 << bitNumber)));
        }
    }
}