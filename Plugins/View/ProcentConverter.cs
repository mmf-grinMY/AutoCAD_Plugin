using System;
using System.Globalization;
using System.Windows.Data;

namespace Plugins.View
{
    /// <summary>
    /// Конвертер размеров
    /// </summary>
    internal class ProcentConverter : IValueConverter
    {
        /// <summary>
        /// Конвертировать в единицы измерения
        /// </summary>
        /// <param name="value">Конвертируемое значение</param>
        /// <param name="targetType">Тип конвертируемого объекта</param>
        /// <param name="parameter">Параметр конвертации</param>
        /// <param name="culture">Региональные параметры конвертации</param>
        /// <returns>Сконвертированное значение</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((double)value is double.NaN)
            {
                return 108;
            }
            return System.Convert.ToDouble(value) * System.Convert.ToDouble(parameter);
        }
        /// <summary>
        /// Конвертировать из единиц измерения
        /// </summary>
        /// <param name="value">Конвертируемое значение</param>
        /// <param name="targetType">Тип конвертируемого объекта</param>
        /// <param name="parameter">Параметр конвертации</param>
        /// <param name="culture">Региональные параметры конвертации</param>
        /// <returns>Сконвертированное обратно значение</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
