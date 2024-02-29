using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins
{
    /// <summary>
    /// Расширения для класса ResultBuffer
    /// </summary>
    static class ResultBufferExtensions
    {
        /// <summary>
        /// Получение XData
        /// </summary>
        /// <param name="buffer">Исходный буфер</param>
        /// <param name="RegAppName">Зарегестрированное имя</param>
        /// <returns></returns>
        public static string GetXData(this ResultBuffer buffer, string RegAppName)
        {
            var flag = false;
            var result = string.Empty;
            foreach (var tv in buffer)
            {
                if (flag)
                {
                    result = tv.Value.ToString();
                    flag = false;
                }
                if ((tv.TypeCode == (short)DxfCode.ExtendedDataRegAppName) && (tv.Value.ToString() == RegAppName))
                {
                    flag = true;
                }
            }
            return result;
        }
    }
}