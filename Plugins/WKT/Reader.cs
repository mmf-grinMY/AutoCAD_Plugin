using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using System.Text.RegularExpressions;
using System;
using System.Net.NetworkInformation;
using NLog;
using System.Security.Policy;
using static Plugins.WKT.RegExp;
using Autodesk.AutoCAD.GraphicsInterface;
using System.Text;
using System.Linq;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics;

namespace Plugins.WKT
{
    //public class Program
    //{
    //    public static void Main(string[] args)
    //    {
    //        string connectionString = @"C:\Users\grinm\Documents\_Работа_\_MapManager_\Plugins\k630f_trans_control.xml";
    //        if (!File.Exists(connectionString)) throw new IOException($"Не обнаружен файл \"{connectionString}\"");
    //        List<DrawParameters> drawParameters = new List<DrawParameters>();
    //        using (IConnection connection = new XmlConnection(connectionString))
    //        {
    //            // Получение корневого элемента документа
    //            XmlElement rowdata = connection.Connect();

    //            XmlNodeList nodeList = rowdata.SelectNodes("//ROW");
    //            if (nodeList != null)
    //            {
    //                foreach (XmlNode row in nodeList)
    //                {
    //                    DrawParameters parameters = new DrawParameters();
    //                    parameters.DrawSettings = JsonSerializer.Deserialize(row.SelectSingleNode("DRAWJSON").InnerText, typeof(DrawSettings)) as DrawSettings;
    //                    parameters.WKT = row.SelectSingleNode("GEOWKT").InnerText;
    //                    drawParameters.Add(parameters);
    //                }
    //            }
    //        }
    //        foreach (var param in drawParameters)
    //        {
    //            Console.WriteLine(param);
    //        }
    //        Console.Read();
    //    }   
    //}

    // MULTILINESTRING((534708.506 5856671.649,534709.175 5856670.851))
    public static class RegExp
    {
        public static readonly Regex line = new Regex(@"\((\d+(\.\d{0,3})? \d+(\.\d{0,3})?,( ?))+\d+(\.\d{0,3})? \d+(\.\d{0,3})?\)");
        public static readonly Regex point = new Regex(@"\d+(\.\d{0,3})? \d+(\.\d{0,3})?");
    }
    public class LineString
    {
        public override string ToString()
        {
            if (Points.Count == 0) return string.Empty;
            StringBuilder builder = new StringBuilder();
            builder.Append("(");
            for (int i = 0; i < Points.Count - 1; i++)
                builder.Append($"{Points[i]},");
            return builder.Append($"{Points.Last<Point>()})").ToString();
        }
        private readonly List<Point> _points = new List<Point>();
        public List<Point> Points => _points;
        private double Parse(string number)
        {
            string[] numbers = number.Split('.');
            return int.Parse(numbers[0])+int.Parse(numbers[1]) * Math.Pow(0.1, numbers[1].Length);
        }
        public LineString(string source)
        {
            if (line.Matches(source).Count == 1)
            {
                MatchCollection match = point.Matches(source);
                foreach (Match item in match)
                {
                    string[] xy = item.Value.Split(' ');
                    double x = Parse(xy[0]);
                    double y = Parse(xy[1]);
                    _points.Add(new Point(x, y));
                }
            }
            else
            {
                throw new ArgumentException("Невозможно преобразовать строку данных в линию!");
            }
        }
    }
    public class MultiLine
    {
        private readonly List<LineString> _lines = new List<LineString>();
        public List<LineString> Lines => _lines;
        public MultiLine(string source)
        {
            MatchCollection lines = line.Matches(source);
            if (lines.Count == 0) throw new Exception();
            foreach (Match line in lines)
            {
                _lines.Add(new LineString(line.Value));
            }
        }
        public override string ToString()
        {
            if (Lines.Count == 0) return string.Empty;
            StringBuilder builder = new StringBuilder();
            builder.Append("(");
            for (int i = 0; i < Lines.Count - 1; i++)
                builder.Append($"{Lines[i]},");
            return builder.Append($"{Lines.Last<LineString>()})").ToString();
        }
    }
    public class MultiLineStrings : MultiLine
    {
        public MultiLineStrings(string source) : base(source) { }
        public override string ToString()
        {
            return "MULTILINESTRING" + base.ToString();
        }
    }
    public class Polygon : MultiLine
    {
        public Polygon(string source) : base(source) { }
        public override string ToString()
        {
            string basedString = base.ToString();
            //return basedString.Equals(string.Empty) ? basedString : "POLYGON" + basedString;
            return "POLYGON" + base.ToString();
        }
    }
    public class Reader
    {
        private static readonly string multiline = "MULTILINESTRING";
        private static readonly string polygon = "POLYGON";
        private static readonly string point = "POINT";
        //private static readonly string coord_regex_expression = @"\d+\.\d{3}";
        //private static readonly string point_regex_expression = string.Format(@"{0} {0}", coord_regex_expression);
        //private static readonly Regex regex_polyline = new Regex(string.Format(@"^MULTILINESTRING\(({0},)*{0}\)$", string.Format(@"\(({0},)+{0}\)", point_regex_expression)));
        //private static readonly Regex regex_point = new Regex(string.Format(@"^POINT\({0}\)$", point_regex_expression));
        //private static readonly Regex regex_polygon = new Regex(string.Format(@"^POLYGON\(\(({0},)*{1}\)\)$", point_regex_expression ,coord_regex_expression));
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

            //if (wktString.Equals(string.Empty)) return points;
            //else if (regex_polyline.IsMatch(wktString))
            //{
            //    int commandLentgth = 17;
            //    int bracesLength = 2;
            //    string str = wktString.Substring(commandLentgth, wktString.Length - commandLentgth - bracesLength);
            //    string[] strings = str.Split(',');
            //    foreach (var item in strings)
            //    {
            //        string[] xy = item
            //            .Replace(".", ",") // Заменяем разделяющие знаки с точек на запятые
            //            .Trim(' ') // Убираем крнечные пробелы
            //            .Split(' '); // Разделяем на координаты точек
            //        if (double.TryParse(xy[0], out double x) && double.TryParse(xy[1], out double y))
            //        {
            //            points.Add(new Point(x, y));
            //        }
            //    }
            //    return points;
            //}
            //else if (regex_point.IsMatch(wktString))
            //{

            //}
            //else if (regex_polygon.IsMatch(wktString))
            //{

            //}
            //else if(wktString.Contains("L"))


            //Regex regex_polyline = new Regex(@"^MULTILINESTRING\((\((\d+\.\d{3} \d+\.\d{3},)+\d+\.\d{3} \d+\.\d{3}\),)*\((\d+\.\d{3} \d+\.\d{3},)+\d+\.\d{3} \d+\.\d{3}\)\)$");
            //Regex regex_polyline = new Regex(string.Format(@"^MULTILINESTRING\(({0},)*{0}\)$", string.Format(@"\(({0},)+{0}\)", string.Format(@"{0} {0}", @"\d+\.\d{3}"))));
            //Match match = regex_polyline.Match(wktString);
            //if (match != null) 
            //{
            //    string str = match.Value;
            //    int commandLentgth = 17;
            //    int bracesLength = 2;
            //    str = str.Substring(commandLentgth, str.Length - commandLentgth - bracesLength);
            //    string[] strings = str.Split(',');
            //    foreach (var item in strings)
            //    {
            //        string[] xy = item
            //            .Replace(".", ",") // Заменяем разделяющие знаки с точек на запятые
            //            .Trim(' ') // Убираем крнечные пробелы
            //            .Split(' '); // Разделяем на координаты точек
            //        if (double.TryParse(xy[0], out double x) && double.TryParse(xy[1], out double y))
            //        {
            //            points.Add(new Point(x, y));
            //        }
            //    }
            //    //Console.WriteLine(strings.ToString1());
            //    return points;
            //}

            //return points;
        }
    }
}