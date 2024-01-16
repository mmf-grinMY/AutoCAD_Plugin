#region Usings

using System;
using Oracle.ManagedDataAccess.Client;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;

#endregion

namespace Plugins
{
    public readonly struct ObjectDispatcherCtorArgs
    {
        public Document Document { get; }
        public string Gorizont { get; }
        public Point3d[] Points { get; }
        public bool IsBound { get; }
        public Func<Draw, Point3d[], DrawParams> Sort { get; }
        public OracleConnection Connection { get; }
        public int Limit { get; }
        public ObjectDispatcherCtorArgs(
            Document doc, 
            string gorizont, 
            Point3d[] points, 
            bool isBound, 
            Func<Draw, Point3d[], DrawParams> sort,
            OracleConnection connection,
            int limit) 
        {
            Document = doc;
            Gorizont = gorizont;
            Points = points;
            IsBound = isBound;
            Sort = sort;
            Connection = connection;
            Limit = limit;
        }
    }
}