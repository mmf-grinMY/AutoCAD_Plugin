#region Usings

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.Text;
using System.Linq;

using static Plugins.WKT.Old.RegExp;

#endregion

namespace Plugins.WKT.Old
{
    public class LineString
    {
        public override string ToString()
        {
            if (Points.Count == 0) return string.Empty;
            StringBuilder builder = new StringBuilder();
            builder.Append("(");
            for (int i = 0; i < Points.Count - 1; i++)
                builder.Append($"{Points[i]},");
            return builder.Append($"{Points.Last()})").ToString();
        }
        private readonly List<Point> _points = new List<Point>();
        public List<Point> Points => _points;
        private double Parse(string number)
        {
            string[] numbers = number.Split('.');
            return numbers.Length == 2 ? int.Parse(numbers[0]) + int.Parse(numbers[1]) * Math.Pow(0.1, numbers[1].Length) : int.Parse(numbers[0]);
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
}