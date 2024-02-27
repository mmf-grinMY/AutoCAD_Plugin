namespace Plugins.Entities
{
    /// <summary>
    /// Расшрение стандартных классов
    /// </summary>
    static class PrimitivesExtensions
    {
        /// <summary>
        /// Конвертация строки в вещественное число
        /// </summary>
        /// <param name="str">Строковое представление числа</param>
        /// <returns>Вещественное число</returns>
        public static double ToDouble(this string str) => System.Convert.ToDouble(str.Replace('.', ','));
        /// <summary>
        /// Конвертация градусов в радианы
        /// </summary>
        /// <param name="degree">Угол в градусах</param>
        /// <returns>Угол в радианах</returns>
        public static double ToRad(this double degree) => degree / 180 * System.Math.PI;
    }
}