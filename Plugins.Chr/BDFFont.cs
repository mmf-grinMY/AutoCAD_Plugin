using System.IO;

namespace Plugins.Chr
{
    /// <summary>
    /// BDF шрифт
    /// </summary>
    public class BDFFont
    {
        #region Private Fields
        /// <summary>
        /// Имя шрифта
        /// </summary>
        private string Name { get; }
        /// <summary>
        /// Описание шрифта
        /// </summary>
        private string Description { get; }
        /// <summary>
        /// Высота над базовой линией
        /// </summary>
        private int Ascent { get; }
        /// <summary>
        /// Высота под базовой линией
        /// </summary>
        private int Descent { get; }
        /// <summary>
        /// Смещение базовой линии
        /// </summary>
        private int Baseline { get; }
        /// <summary>
        /// Символы
        /// </summary>
        private BitMap[] Chars { get; }
        /// <summary>
        /// Главная версия шрифта
        /// </summary>
        private int Major { get; }
        /// <summary>
        /// Подверсия шрифта
        /// </summary>
        private int Minor { get; }
        /// <summary>
        /// Минимальная координата точки шрифта по оси X
        /// </summary>
        private int minX = 0;
        /// <summary>
        /// Максимальная координата точки шрифта по оси X
        /// </summary>
        private int maxX = 0;
        /// <summary>
        /// Минимальная координата точки шрифта по оси Y
        /// </summary>
        private int minY = 0;
        /// <summary>
        /// Максимальная координата точки шрифта по оси Y
        /// </summary>
        private int maxY = 0;
        #endregion

        #region Ctors
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="description">Описание шрифта</param>
        /// <param name="ascent">Высота над уровнем базовой линии</param>
        /// <param name="descent">Высота под уровнем базовой линии</param>
        /// <param name="baseline">Смещение базовой линии</param>
        /// <param name="chars">Символы</param>
        /// <param name="major">Главная версия шрифта</param>
        /// <param name="minor">Подверсия шрифта</param>
        /// <param name="name">Имя шрифта</param>
        public BDFFont(string description, int ascent, int descent, int baseline, BitMap[] chars, int major, int minor, string name)
        {
            Description = description;
            Ascent = ascent;
            Descent = descent;
            Baseline = baseline;
            Chars = chars;
            Major = major;
            Minor = minor;
            Name = name;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Вычисление граничной рамки для всех символов
        /// </summary>
        private void CalculateBoundingBox()
        {
            for (int i = 0; i < Chars.Length; i++)
            {
                if (minX == 0 && maxX == 0)
                {
                    minX = Chars[i].OffsetX;
                    maxX = Chars[i].MaxX;
                }
                else
                {
                    if (minX > Chars[i].OffsetX) 
                        minX = Chars[i].OffsetX;
                    if (maxX < Chars[i].MaxX)
                        maxX = Chars[i].MaxX;
                }
                if (minY == 0 && maxY == 0)
                {
                    maxY = Chars[i].OffsetY;
                    minY = Chars[i].MinY;
                }
                else
                {
                    if (minY > Chars[i].MinY)
                        minY = Chars[i].MinY;
                    if (maxY < Chars[i].OffsetY)
                        maxY = Chars[i].OffsetY;
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Запись шрифта в файл BDF
        /// </summary>
        /// <param name="filepath">Путь файла для записи</param>
        public void Dump(string filepath)
        {
            using (var writer = new StreamWriter(filepath))
            {
                writer.WriteLine($"STARTFONT {Major}.{Minor}");
                writer.WriteLine($"FONT {Name}");
                writer.WriteLine($"COMMENT {Description.Replace("\n", "\nCOMMENT ")}");
                writer.WriteLine("SIZE 10 75 75");
                CalculateBoundingBox();
                writer.WriteLine($"FONTBOUNDINGBOX {maxX - minX + 1} {maxY - minY + 1} {minX} {minY}");
                writer.WriteLine($"CHARS {Chars.Length}");
                for (int i = 0; i < Chars.Length; i++)
                {
                    writer.Write(new BDFChar(Chars[i], i + 1).Dump(Ascent - Descent));
                }
                writer.WriteLine("ENDFONT");
                writer.Close();
            }
        }
        #endregion
    }
}
