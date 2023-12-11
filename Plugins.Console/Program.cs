using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Aspose.Gis.Geometries;
using Oracle.ManagedDataAccess.Client;
using Newtonsoft.Json;
using Plugins;
using System.Windows;
using Newtonsoft.Json.Linq;

internal class Program
{
    public static void Main(string[] args)
    {
        string dbName = "k630f";
        DrawParameters draw;
        using (var connection = new OracleConnection("Data Source=data-pc/GEO;Password=g;User Id=g;"))
        {
            connection.Open();
            using (var reader = new OracleCommand($"SELECT drawjson, geowkt, paramjson FROM {dbName}_trans_open", connection).ExecuteReader())
            {
                reader.Read();
                draw = new DrawParameters()
                {
                    DrawSettings = JObject.Parse(reader.GetString(0))
                };
                if (draw.DrawSettings["DrawType"].ToString() != "Empty")
                {
                    draw.WKT = reader.GetString(1);
                    draw.Param = JObject.Parse(reader.GetString(2));
                }
            }
        }

        switch (draw.DrawSettings["DrawType"].ToString())
        {
            case "Polyline":
                {
                    Console.WriteLine(Convert.ToDouble(draw.Param["LeftBound"].ToString().Replace("_", ",")));
                }
                break;
            default: break;
        }
        Console.Read();
    }
}