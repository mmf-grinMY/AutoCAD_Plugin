namespace Plugins
{
    /// <summary>
    /// Конвертер цветов
    /// </summary>
    public static class ColorConverter
    {
        /// <summary>
        /// Конвертация из цвета MapManager в AutoCAD Color
        /// </summary>
        /// <param name="color">MapManager color</param>
        /// <returns>AutoCAD color</returns>
        public static Autodesk.AutoCAD.Colors.Color FromMMColor(int color) 
        {
            const int maskRed = 0xFF;
            const int maskGreen = 0xFF00;
            const int maskBlue = 0xFF0000;
            byte red = System.Convert.ToByte(color & maskRed);
            byte green = System.Convert.ToByte((color & maskGreen) >> 8);
            byte blue = System.Convert.ToByte((color & maskBlue) >> 16);
            return Autodesk.AutoCAD.Colors.Color.FromRgb(red, green, blue);
        }
    }
}