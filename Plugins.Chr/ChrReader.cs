using System;
using System.Text;
using System.Linq;

namespace Plugins.Chr
{
    /// <summary>
    /// Читатель формата *.CHR
    /// </summary>
    public class ChrReader
    {
        /// <summary>
        /// Команда для отрисоввки символа
        /// </summary>
        internal struct SymbolPoint
        {
            /// <summary>
            /// Координата
            /// </summary>
            private byte _byte;
            /// <summary>
            /// Координата пикселя символа
            /// </summary>
            public byte Byte
            {
                get => _byte;
                set
                {
                    _byte = value;
                    Opcode = MyBitConverter.Bit(Byte, 7) == 1;
                    Sign = MyBitConverter.Bit(Byte, 6) == 1;
                    _byte = MyBitConverter.BitReset(Byte, 7);
                    _byte = MyBitConverter.BitReset(Byte, 6);
                    New = (sbyte)Byte;
                }
            }
            /// <summary>
            /// Текущая позиция пера
            /// </summary>
            public int Current { get; set; }
            /// <summary>
            /// Стартовая позиция пера
            /// </summary>
            public int Start { get; set; }
            /// <summary>
            /// Буфферное значение
            /// </summary>
            public int Buffer { get; set; }
            /// <summary>
            /// Новая координата пера
            /// </summary>
            public sbyte New { get; set; }
            /// <summary>
            /// Опкод операции
            /// </summary>
            public bool Opcode { get; set; }
            /// <summary>
            /// Знак координаты
            /// </summary>
            public bool Sign { get; set; }
        }
        /// <summary>
        /// Преобразование бинарной записи из файла в BDFFont
        /// </summary>
        /// <param name="filepath">Путь к файлу</param>
        /// <returns>BDF шрифт, записанный в файл</returns>
        /// <exception cref="BadFileFormatException">Вызывается, если считыаемый файл не явялется форматом BGI</exception>
        public static BDFFont Read(string filepath)
        {
            using (var br = new MyBinaryReader(filepath))
            {
                if (!Enumerable.SequenceEqual(br.ReadBytes(4), new byte[] { 0x50, 0x4B, 0x08, 0x08 }))
                {
                    throw new BadFileFormatException("Not a Borland CHR font.");
                }

                byte buffer;
                string descripton = string.Empty;
                while ((buffer = br.ReadByte()) != 0x1A)
                {
                    descripton += Encoding.ASCII.GetString(new byte[] { buffer });
                }
                ushort headerSize = MyBitConverter.ToUInt16(br.ReadBytes(2));
                string fontId = Encoding.ASCII.GetString(br.ReadBytes(4), 0, 4);
                ushort dataSize = MyBitConverter.ToUInt16(br.ReadBytes(2));
                ushort fontMajorVersion = MyBitConverter.ToUInt16(br.ReadBytes(2));
                ushort fontMinorVersion = MyBitConverter.ToUInt16(br.ReadBytes(2));
                br.Seek(headerSize);

                if (br.ReadByte() != 0x2B)
                {
                    throw new BadFileFormatException("Not a stroked font.");
                }

                ushort characterCount = MyBitConverter.ToUInt16(br.ReadBytes(2));
                br.Skip();
                byte startingChar = br.ReadByte();
                ushort strokeDefinitonOffset = MyBitConverter.ToUInt16(br.ReadBytes(2));
                br.Skip();
                sbyte originToCapital = (sbyte)br.ReadByte();
                sbyte originToBaseline = (sbyte)br.ReadByte();
                sbyte originToDescender = (sbyte)br.ReadByte();
                br.Skip(5);
                ushort[] characterDefitionOffset = new ushort[characterCount];
                for (int i = 0; i < characterCount; i++)
                {
                    characterDefitionOffset[i] = MyBitConverter.ToUInt16(br.ReadBytes(2));
                }
                byte[] characterWidths = new byte[characterCount];
                for (int j = 0; j < characterCount; j++)
                {
                    characterWidths[j] = br.ReadByte();
                }
                int characterHeight = originToCapital * originToDescender > 0 ? Math.Abs(originToCapital - originToDescender) : 128 - originToCapital + originToDescender;
                BitMap[] chars = new BitMap[characterCount];
                byte finished;
                var x = new SymbolPoint
                {
                    Start = 10,
                    Buffer = 5
                };
                var y = new SymbolPoint
                {
                    Start = 10,
                    Buffer = 10
                };
                for (int i = 0; i < characterCount; i++)
                {
                    chars[i] = new BitMap(0, 0);
                    int address = characterDefitionOffset[i] + strokeDefinitonOffset + headerSize - 1;
                    if (!br.Seek(Convert.ToUInt64(address)))
                        continue;
                    finished = 0;
                    do
                    {
                        x.Byte = br.ReadByte();
                        y.Byte = br.ReadByte();
                        if (x.Sign)
                        {
                            x.New = Convert.ToSByte(Convert.ToString(x.Byte, 2).PadLeft(8, '1'), 2);
                        }
                        if (y.Sign)
                        {
                            y.New = Convert.ToSByte(64 - y.New);
                            y.New = Convert.ToSByte(y.New * -1);
                        }
                        y.New = (sbyte)(64 - y.New);
                        if (!x.Opcode)
                        {
                            if (!y.Opcode)
                            {
                                x.Start = x.Start + characterWidths[i] + x.Buffer;
                                if (x.Start > 950)
                                {
                                    x.Start = 10;
                                    y.Start = y.Start + characterHeight + y.Buffer;
                                }
                                finished = 1;
                            }
                            else
                            {
                                // TODO: Доабвить обработку этого случая
                            }
                        }
                        else
                        {
                            if (!y.Opcode)
                            {
                                x.Current = x.Byte;
                                y.Current = y.Byte;
                            }
                            else
                            {
                                int x1, x2, y1, y2;
                                x1 = x.Current - x.Start;
                                y1 = 64 - (y.Current - y.Start);
                                x2 = x.New;
                                y2 = 64 - y.New;
                                chars[i].Line(x1, y1, x2, y2);
                            }
                            x.Current = x.Start + x.New;
                            y.Current = y.Start + y.New;
                        }
                    }
                    while (finished != 1);
                }

                return new BDFFont(descripton, originToCapital, originToDescender, originToBaseline, chars, fontMajorVersion, fontMinorVersion, fontId);
            }
        }
    }
}
