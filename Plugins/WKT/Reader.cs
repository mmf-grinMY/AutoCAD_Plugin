using System;

namespace Plugins.WKT.Old
{
    public class Reader
    {
        private static readonly string multiline = "MULTILINESTRING";
        private static readonly string polygon = "POLYGON";
        private static readonly string point = "POINT";
        public static object Read(string wktString, DrawType drawType)
        {

            switch (drawType)
            {
                case DrawType.Empty:
                    return null;
                case DrawType.Polyline: 
                    if (wktString.Contains(multiline))
                    {
                        return new MultiLineStrings(wktString);
                    }
                    else if (wktString.Contains(polygon))
                    {
                        return new Polygon(wktString);
                    }
                    else if (wktString.Equals(string.Empty))
                    {
                        return null;
                    }
                    else
                    {
                        throw new ArgumentException("Неизвестная геометрия!");
                    }
                case DrawType.LabelDrawParams:
                    if (wktString.Contains(point))
                    {
                        return new Point(wktString);
                    }
                    else
                    {
                        throw new ArgumentException("Неизвестная геометрия!");
                    }
                case DrawType.BasicSignDrawParams:
                    if (wktString.Contains(point))
                    {
                        return new Point(wktString);
                    }
                    else
                    {
                        throw new ArgumentException("Неизвестная геометрия!");
                    }
                default:
                    return null;
            }
        }
    }
}