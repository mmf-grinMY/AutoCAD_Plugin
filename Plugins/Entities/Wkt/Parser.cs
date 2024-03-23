using System.Text.RegularExpressions;

using APolyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using Autodesk.AutoCAD.Geometry;

namespace Plugins.Entities.Wkt
{
    static class Parser
    {
        readonly static Regex line;
        readonly static Regex point;
        static Parser()
        {
            line = new Regex(@"\((\d+(\.\d{0,3})? \d+(\.\d{0,3})?,( ?))+\d+(\.\d{0,3})? \d+(\.\d{0,3})?\)");
            point = new Regex(@"\d+(\.\d{0,3})? \d+(\.\d{0,3})?");
        }
        public static APolyline[] Parse(string wkt)
        {
            var matches = line.Matches(wkt);
            var lines = new APolyline[matches.Count];

            for (int i = 0; i < lines.Length; ++i)
            {
                lines[i] = new APolyline();
                var match = point.Matches(matches[i].Value);

                for (int j = 0; j < match.Count; ++j)
                {
                    var coords = match[j].Value.Split(' ');
                    lines[i].AddVertexAt(j, new Point2d(coords[0].ToDouble()
                        * Constants.SCALE, coords[1].ToDouble() * Constants.SCALE), 0, 0, 0);
                }
            }

            return lines;
        }
        public static Point3d ParsePoint(string wkt)
        {
            var coords = point.Match(wkt).Value.Split(' ');

            return new Point3d(coords[0].ToDouble() * Constants.SCALE,
                               coords[1].ToDouble() * Constants.SCALE,
                               0);
        }
    }
}
