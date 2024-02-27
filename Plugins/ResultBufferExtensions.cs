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
        /// <param name="rb">Исходный буфер</param>
        /// <param name="RegAppName">Зарегестрированное имя</param>
        /// <returns></returns>
        public static string GetXData(this ResultBuffer rb, string RegAppName)
        {
            var proc_fl_1 = false;
            var result = string.Empty;
            foreach (var tv in rb)
            {
                if (proc_fl_1)
                {
                    result = tv.Value.ToString();
                    proc_fl_1 = false;
                }
                if ((tv.TypeCode == (int)DxfCode.ExtendedDataRegAppName) && (tv.Value.ToString() == RegAppName))
                {
                    proc_fl_1 = true;
                }
            }
            return result;
        }
    }
}