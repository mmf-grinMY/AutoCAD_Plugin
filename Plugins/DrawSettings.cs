using System;
using System.Windows;

namespace Plugins
{
    public class DrawSettings
    {
        private static readonly DrawSettings _empty = new DrawSettings() { DrawType = "Empty" };
        public static DrawSettings Empty => _empty;
        private DrawType _drawType;
        public string DrawType
        {
            set
            {
                switch (value)
                {
                    case "Empty":
                        _drawType = Plugins.DrawType.Empty;
                        break;
                    case "Polyline":
                        _drawType = Plugins.DrawType.Polyline;
                        break;
                    case "LabelDrawParams":
                        _drawType = Plugins.DrawType.LabelDrawParams;
                        break;
                    case "BasicSignDrawParams":
                        _drawType = Plugins.DrawType.BasicSignDrawParams;
                        break;
                    case "TMMTTFSignDrawParams":
                        _drawType = Plugins.DrawType.TMMTTFSignDrawParams;
                        break;
                    default: MessageBox.Show(value); break; //throw new InvalidOperationException();
                }
            }
        }
        public DrawType GetDrawType { get { return _drawType; } }
        public int PenColor { get; set; }
        public int BrushColor { get; set; }
        public int BrushBkColor { get; set; }
        public int Width { get; set; }
        private bool _closed;
        public bool GetClosed => _closed;
        public string Closed
        {
            set
            {
                switch (value)
                {
                    case "false":
                        _closed = false;
                        break;
                    case "true":
                        _closed = true;
                        break;
                    default: throw new InvalidOperationException();
                }
            }
        }
        public string BitmapName { get; set; }
        public int BitmapIndex { get; set; }
        private bool _transparent;
        public bool GetTransparent => _transparent;
        public string Transparent
        {
            set
            {
                switch (value)
                {
                    case "false":
                        _transparent = false;
                        break;
                    case "true":
                        _transparent = true;
                        break;
                    default: throw new InvalidOperationException();
                }
            }
        }
        public int PenStyle { get; set; }
        public override string ToString()
        {
            return $"{GetDrawType};{PenColor};{BrushColor};{BrushBkColor};{Width};{GetClosed};{BitmapName};{BitmapIndex};{GetTransparent};{PenStyle}";
        }
    }
}