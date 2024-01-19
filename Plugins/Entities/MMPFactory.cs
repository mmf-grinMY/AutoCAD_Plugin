using System;

using Autodesk.AutoCAD.DatabaseServices;

namespace Plugins
{
    static class MMPFactory
    {
        public static MMPEntity Create(Database db, DrawParams draw, Box box )
        {
            switch (draw.DrawSettings["DrawType"].ToString())
            {
                case "Polyline":
                    {
                        switch (draw.Geometry)
                        {
                            case Aspose.Gis.Geometries.MultiLineString _: return new MMPPolyline(db, draw, box);
                            case Aspose.Gis.Geometries.Polygon _: return new MMPPolygon(db, draw, box);
                            default: throw new NotImplementedException();
                        }
                    }
                case "BasicSignDrawParams":
                case "TMMTTFSignDrawParams":
                    {
                        if (draw.Geometry is Aspose.Gis.Geometries.Point point)
                        {
                            return new MMPSign(db, draw, box);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                case "LabelDrawParams":
                    {
                        if (draw.Geometry is Aspose.Gis.Geometries.Point point)
                        {
                            return new MMPText(db, draw, box);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                default: throw new NotImplementedException();
            }
        }
    }
}