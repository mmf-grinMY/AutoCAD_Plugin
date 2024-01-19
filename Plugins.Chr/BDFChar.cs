using System;
using System.Text;

namespace Plugins.Chr
{
    /// <summary>
    /// BDF символ
    /// </summary>
    public class BDFChar
    {
        #region Private Fields
        /// <summary>
        /// Номер символа в кодировке ASCII
        /// </summary>
        private int Encoding { get; }
        /// <summary>
        /// Высота символа
        /// </summary>
        private int Height { get; }
        /// <summary>
        /// Длина битового поля
        /// </summary>
        private int Width { get; }
        /// <summary>
        /// Смещение по оси X
        /// </summary>
        private int OffsetX { get; }
        /// <summary>
        /// Смещение по оси Y
        /// </summary>
        private int OffsetY { get; }
        /// <summary>
        /// Битовое поле пикселей
        /// </summary>
        private byte[,] Field { get; }
        /// <summary>
        /// Ширина символа
        /// </summary>
        private int Length { get; }
        #endregion

        #region Ctors
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="map">Битовое поле символа</param>
        /// <param name="encoding">Номер символа в кодировке ASCII</param>
        public BDFChar(BitMap map, int encoding) 
        {
            Field = map.Field;
            Height = map.Height;
            Width = map.Width;
            Length = map.AbsLength;
            OffsetX = map.OffsetX;
            OffsetY = map.OffsetY;
            Encoding = encoding;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Сохранение символа в формате BDF
        /// </summary>
        /// <param name="em">Общая высота символа</param>
        /// <param name="name">Имя символа</param>
        /// <returns>Строковая запись символа в формате BDF</returns>
        public string Dump(int em, string name = "")
        {
            StringBuilder sb = new StringBuilder();
            if (name == string.Empty)
                name = "char" + Encoding;
            sb.AppendFormat("STARTCHAR {0}\n", name);
            sb.AppendFormat("ENCODING {0}\n", Encoding);
            sb.AppendFormat("SWIDTH {0} 0\n", Length * 1000 / em);
            sb.AppendFormat("DWIDTH {0} 0\n", Length);
            sb.AppendFormat("BBX {0} {1} {2} {3}\n", Length, Height, 0/*OffsetX*/, -Height/2/*OffsetY*/);
            sb.AppendLine("BITMAP");
            for (int i = 0; i < Height; i++)
            {
                for (int j = 0; j < Width; j++)
                {
                    sb.Append(Convert.ToString(Field[i, j], 16).PadLeft(2, '0'));
                }
                sb.AppendLine();
            }
            sb.AppendLine("ENDCHAR");
            return sb.ToString();
        }
        #endregion
    }
}
