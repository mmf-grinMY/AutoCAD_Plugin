using Autodesk.AutoCAD.Geometry;
using System.Net.NetworkInformation;

namespace Plugins
{
    public static class PointExtension
    {
        public static Point3d ToPoint3d(this Point point)
        {
            return new Point3d(point.X, point.Y, point.Z);
        }
        public static Point2d ToPoint2d(this Point point)
        {
            return new Point2d(point.X, point.Y);
        }
    }
}