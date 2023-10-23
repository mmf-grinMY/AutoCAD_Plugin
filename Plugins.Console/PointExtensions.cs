using System.Collections.Generic;
using System.Text;
//using Newtonsoft.Json;

namespace Plugins.ConsoleProgram
{
    public static class PointExtensions
    {
        public static string MyToString(this List<Point> points)
        {
            if (points.Count == 0)
                return string.Empty;
            StringBuilder builder = new StringBuilder();
            builder.Append("[ ");
            for (int i = 0; i < points.Count - 1; i++)
            {
                builder.Append($"({points[i].X}, {points[i].Y}), ");
            }
            int lastIndex = points.Count - 1;
            builder.Append($"({points[lastIndex].X}, {points[lastIndex].Y}) ]");
            return builder.ToString();
        }
    }
}
