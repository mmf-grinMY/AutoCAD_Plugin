using System;

namespace Plugins.Chr
{
    /// <summary>
    /// Битовое поле символа
    /// </summary>
    public class BitMap : ICloneable
    {
        #region Inner Classes
        /// <summary>
        /// Двумерная точка
        /// </summary>
        class InnerPoint
        {
            /// <summary>
            /// Координата по оси X
            /// </summary>
            public int x;
            /// <summary>
            /// Коордианата по оси Y
            /// </summary>
            public int y;
        }
        #endregion

        #region Private Fields 
        /// <summary>
        /// Ширина символа внутри неполного байта
        /// </summary>
        private int byteInnerLength;
        /// <summary>
        /// Битовое поле для хранения символа
        /// </summary>
        private byte[,] field;
        /// <summary>
        /// Точка смещения
        /// </summary>
        private readonly InnerPoint offset;
        #endregion

        #region Private Properties
        /// <summary>
        /// Индексация битового поля
        /// </summary>
        /// <param name="x">Координата по оси X</param>
        /// <param name="y">Координата оп оси Y</param>
        /// <returns>Значение пикселя по координатам (x, y)</returns>
        private byte this[int x, int y]
        {
            set
            {
                int row = OffsetY - y;
                int col = (x - OffsetX) / 8;
                int pos = (7 - (x - OffsetX) % 8);
                field[row, col] = (byte)(field[row, col] | value << pos);
            }
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Битовое поле для хранения символа
        /// </summary>
        public byte[,] Field
        {
            get { return field; }
            private set { field = value; }
        }
        /// <summary>
        /// Смещение символа по оси X
        /// </summary>
        public int OffsetX => offset.x;
        /// <summary>
        /// Смещение смивола по оси Y
        /// </summary>
        public int OffsetY => offset.y;
        /// <summary>
        /// Высота символа
        /// </summary>
        public int Height => field.GetUpperBound(0) + 1;
        /// <summary>
        /// Ширина битового поля
        /// </summary>
        public int Width => field.Length / Height;
        /// <summary>
        /// Ширина символа внутри неполного байта
        /// </summary>
        public int ByteInnerLength => byteInnerLength;
        /// <summary>
        /// Координата по оси X самого высокого пикселя
        /// </summary>
        public int MaxX => OffsetX + AbsLength - 1;
        /// <summary>
        /// Координата по оси Y самого низкого пикселя
        /// </summary>
        public int MinY => OffsetY - Height + 1;
        /// <summary>
        /// Действительная ширина символа
        /// </summary>
        public int AbsLength => (Width - 1) * 8 + ByteInnerLength;
        #endregion

        #region Ctors
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="offsetX">Смещение оп оси X</param>
        /// <param name="offsetY">Смещение оп оси Y</param>
        public BitMap(int offsetX, int offsetY)
        {
            Field = new byte[1, 1] { { 0b00000000 } };
            offset = new InnerPoint() { x = offsetX, y = offsetY };
            byteInnerLength = 0;
        }
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="field">Битовое поле</param>
        /// <param name="ox">Смещение по оси X</param>
        /// <param name="oy">Смещение оп оси Y</param>
        /// <param name="width">Длина символа</param>
        internal BitMap(byte[,] field, int ox, int oy, int width)
        {
            Field = field;
            offset = new InnerPoint() { x = ox, y = oy };
            byteInnerLength = width;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Добавить точку (x, y)
        /// </summary>
        /// <param name="x">Координата по оси X</param>
        /// <param name="y">Координата по оси Y</param>
        public void AddPoint(int x, int y)
        {
            if (byteInnerLength != 0) 
            {
                if (x < OffsetX)
                {
                    int w = AbsLength + OffsetX - x; // Количество занятых битов
                    int b = w % 8 == 0 ? 8 : w % 8;
                    int b_w = (int)Math.Ceiling(w / 8.0);
                    var arr = new byte[Height, b_w];
                    for (int i = 0; i < Height; i++)
                    {
                        for (int j = 0; j < AbsLength; j++)
                        {
                            int byteIndex = j / 8;
                            int arr_row_index = (j + OffsetX - x) / 8;
                            int position = 7 - j % 8;
                            arr[i, arr_row_index] = (byte)(arr[i, arr_row_index] | (field[i, byteIndex] >> position) << (7 - (j + OffsetX - x) % 8));
                        }
                    }
                    Field = arr;
                    offset.x = x;
                    byteInnerLength = b;
                }
                else if (OffsetX + AbsLength <= x)
                {
                    int w = x - OffsetX + 1;
                    int b = w % 8 == 0 ? 8 : w % 8;
                    int b_w = (int)Math.Ceiling(w / 8.0);
                    var arr = new byte[Height, b_w];
                    for (int i = 0; i < Height; i++)
                    {
                        for (int j = 0; j < Width; j++)
                        {
                            arr[i, j] = field[i, j];
                        }
                    }
                    Field = arr;
                    byteInnerLength = b;
                }

                if (y < OffsetY - Height + 1)
                {
                    int h = OffsetY - y + 1;
                    var arr = new byte[h, Width];
                    for (int i = 0; i < Height; i++)
                    {
                        for (int j = 0; j < Width; j++)
                        {
                            arr[i, j] = field[i, j];
                        }
                    }
                    Field = arr;
                }
                else if (y > OffsetY)
                {
                    int dy = y - OffsetY;
                    int h = Height + dy;
                    var arr = new byte[h, Width];
                    for (int i = 0; i < Height; i++)
                    {
                        for (int j = 0; j < Width; j++)
                        {
                            arr[i + dy, j] = field[i, j];
                        }
                    }
                    Field = arr;
                    offset.y = y;
                }
            }
            else
            {
                offset.x = x;
                offset.y = y;
                byteInnerLength = 1;
            }
        }
        /// <summary>
        /// Нарисовать линию (x1, y1) -> (x2, y2)
        /// </summary>
        /// <param name="x1">Координата начальной точки по оси X</param>
        /// <param name="y1">Координата начальной точки по оси Y</param>
        /// <param name="x2">Координата конечной точки по оси X</param>
        /// <param name="y2">Координата конечной точки по оси Y</param>
        public void Line(int x1, int y1, int x2, int y2)
        {
            AddPoint(x1, y1);
            AddPoint(x2, y2);
            int
                counter = 0,
                dx = Math.Abs(x2 - x1),
                sx = x1 < x2 ? 1 : -1,
                dy = Math.Abs(y2 - y1),
                sy = y1 < y2 ? 1 : -1,
                err = (dx > dy ? dx : -dy) / 2,
                e2;
            while (true)
            {
                this[x1, y1] = 1;

                counter++;

                if (x1 == x2 && y1 == y2)
                    break;
                e2 = err;
                if (e2 > -dx)
                {
                    err -= dy;
                    x1 += sx;
                }
                if (e2 < dy)
                {
                    err += dx;
                    y1 += sy;
                }
            }
        }
        /// <summary>
        /// Клонировать объект
        /// </summary>
        /// <returns>Полный клон объекта</returns>
        public object Clone()
        {
            var tmp = new BitMap(OffsetX, OffsetY)
            {
                byteInnerLength = byteInnerLength,
                field = new byte[Height, Width]
            };
            for (int i = 0; i < Height; i++)
            {
                for (int j = 0; j < Width; j++)
                {
                    tmp.field[i, j] = field[i, j];
                }
            }
            return tmp;
        }
        #endregion
    }
}
