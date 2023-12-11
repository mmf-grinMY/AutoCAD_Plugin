using static Plugins.WKT.Old.RegExp;

namespace Plugins.WKT.Old
{
    public class Point
    {
        public Point(string source)
        {
            string[] xy = point.Match(source).Value.Replace('.', ',').Split(' ');
            if (double.TryParse(xy[0], out double x) && double.TryParse(xy[1], out double y))
            {
                X = x;
                Y = y;
                Z = 0;
            }
        }
        public Point(double x, double y, double z = 0)
        {
            X = x; 
            Y = y; 
            Z = z;
        }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public override string ToString()
        {
            return Z == 0.0 ? $"{X} {Y}".Replace(',', '.') : $"{X} {Y} {Z}".Replace(',', '.');
        }
        public override bool Equals(object obj)
        {
            if (obj is Point p)
            {
                if (p == this) return true;
                else if (p == null) return false;
                else return p.X == this.X && p.Y == this.X && p.Z == this.Z;
            }
            else
            { 
                return false; 
            }
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}