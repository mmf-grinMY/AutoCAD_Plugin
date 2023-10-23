using System;
using Plugins;
using System.Collections.Generic;
using System.Linq;

internal class Program
{
    public static void Main(string[] args)
    {
        var draws = Commands.LoadDataFromDB("SYS", "SYSTEM", "localhost/XEPDB1", "SYSDBA");
        HashSet<string> sublayers = new HashSet<string>();
        string sublayer = string.Empty;
        foreach (var draw in draws)
        {
            if (draw.SubleyerGUID != string.Empty)
            {
                if (sublayer != draw.SubleyerGUID)
                {
                    if (!sublayers.Contains(draw.SubleyerGUID))
                    {
                        //TODO: Create new Layer
                        sublayers.Add(draw.SubleyerGUID);
                    }
                    sublayer = draw.SubleyerGUID;
                }
                //TODO: Drawing in current Layer
            }
        }
        foreach (var guid in sublayers)
        {
            Console.WriteLine(guid);
        }
        Console.Read();
    }
}