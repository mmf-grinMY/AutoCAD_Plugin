
// #define CONVERTER // Конвертер из CHR в BDF
// #define DEF_READER // Чтение файла с шаблоном линий
//#define LINE_TYPE_READER
//#define LINE_TYPE_PARSER
//#define DB_FULL // Проверка базы данных на целостность
//#define CONF_TO_XML // Преобразование conf файла MapManager в xml формат

// GEOServer ГИС - система в браузере

#if CONVERTER
using Plugins.Chr;

const string chrFilePath = @"C:\Users\grinm\Documents\_Job_\_MapManager_\Programs\MapMan\Fonts\pnt.chr";
var path = chrFilePath.Split('\\');
var name = path[^1].Replace(".chr", ".new.bdf");
const string outputRoot = @"C:\Users\grinm\Desktop\_AutoCAD_\Fonts";
var font = ChrReader.Read(chrFilePath);
font.Dump(Path.Combine(outputRoot, name));
Console.WriteLine("Закончена запись шрифта!");
#endif

#if DEF_READER
using Plugins.Chr.Lines;

const string filePath = @"C:\Users\grinm\Documents\_Job_\_MapManager_\Programs\MapMan\Fonts\SYMBOL.DEF";
DefConverter.ToJson(filePath, filePath + ".new.json");
Console.WriteLine("Reader закончил свою работу!");

#endif

#if LINE_TYPE_READER
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml;

const string path = @"C:\Users\grinm\Documents\_Job_\_MapManager_\Programs\MapMan\Fonts\LINES.DEF.json";

using (var reader = new StreamReader(path))
{
    string content = reader.ReadToEnd();
    reader.Close();

    JArray root = JArray.Parse(content) ?? throw new ArgumentNullException("", "Невозможно представить распарсить массив!");
    Console.WriteLine(root.First(x => x.Value<string>("GUID") == "{E5550E60-FE78-48AB-8128-54B7914502F9}"));
}
#endif

#if LINE_TYPE_PARSER

using System.IO;
using System.Text.Json;

const string testJson1 = """
	{ "name": "ВНК1",
	"UnionThreshold": "-1",
	"GUID": "{99D45D60-AF4F-11D4-9B0E-00D0B70B2AC1}",
	"ID": "47",
	"StartEdge": {},
	"EndEdge": {
		"Length": "2",
		"AxisLines": {
			"0": {
				"Begin": "0",
				"End": "2",
				"Indent": "0",
				"Color": "Blue16",
				"Width": "0.6"
			}
		}
	},
	"Bypass": {
		"Length": "11",
		"AxisLines": {
			"0": {
				"Begin": "0",
				"End": "7",
				"Indent": "0",
				"Color": "Blue16",
				"Width": "0.6"
			}
		},
		"Decorations": {
			"Group 1": {
				"FixPoint": "9",
				"1": {
					"Type": "FilledCircle",
					"Color": "Blue16",
					"BkColor": "Blue16",
					"Center": "0, 0",
					"Radius": "0.3",
					"Width": "0.3"
				}
			}
		}
	},
	"UnionForward": {},
	"UnionReverse": {}
	}
	"""
;

const string path = @"C:\Users\grinm\Documents\_Job_\_MapManager_\Programs\MapMan\Fonts\LINES.DEF.json";

using var reader = new StreamReader(path);
using JsonDocument doc = JsonDocument.Parse(reader.BaseStream);
JsonElement root = doc.RootElement;

Console.WriteLine(root.GetProperty("LineStyles").GetArrayLength());

#endif

#if DB_FULL

using Aspose.Gis.Geometries;
using Oracle.ManagedDataAccess.Client;

using (OracleConnection connection = new("Data Source=data-pc/GEO;Password=g1;User Id=g;Connection Timeout = 360;"))
{
    Point inner = new(8644.532, 24683.346);
	const double MAX_DISTANCE = 150;

	double Distance(Point point)
	{
		return Math.Sqrt(Math.Pow(inner.X - point.X, 2) + Math.Pow(inner.Y - point.Y, 2));
	}

	connection.Open();
    using var reader = new OracleCommand(
"""
SELECT layername, sublayername, drawjson, geowkt
FROM 
(
    SELECT b.layername, b.sublayername, a.geowkt, a.drawjson
    FROM k450e_trans_clone a
    JOIN k450e_trans_open_sublayers b 
    ON a.sublayerguid = b.sublayerguid
)
WHERE drawjson NOT LIKE '%"LabelDrawParams"%'
AND drawjson NOT LIKE '%TMMTTFSignDrawParams%'
""", connection).ExecuteReader();
start:
    while (reader.Read())
    {
		if (reader.IsDBNull(3))
			continue;
        string wkt = reader.GetString(3);
        var geometry = Geometry.FromText(wkt);
        switch (geometry)
        {
            case Polygon polygon:
                {
                    polygon = geometry as Polygon ?? throw new ArgumentNullException(nameof(geometry));
                    var ring = polygon.ExteriorRing;
					foreach (Point point in ring)
					{
						if (point.Y > 500_000)
							break;

						if (Distance(point) < MAX_DISTANCE)
						{
							Console.WriteLine(polygon.AsText());
                            goto start;
                        }
                    }
                }
                break;
            case MultiLineString multiline:
                {
					foreach (LineString line in multiline)
					{
						foreach (Point point in line)
						{
                            if (point.Y > 500_000)
                                break;

							if (Distance(point) < MAX_DISTANCE)
							{
								Console.WriteLine(multiline.AsText());
								goto start;
							}
                        }
					}
                }
                break;
            default: throw new NotImplementedException();
        }
    }
    Console.WriteLine("Все данные проверены!");
}
#endif

#if CONF_TO_XML
const string root = @"C:\Users\grinm\Documents\_Job_\_MapManager_\Programs\MapMan\UserData";
const string pattern_conf = "Pattern.conf";

string content = "";

using (StreamReader sr = new(Path.Combine(root, pattern_conf)))
{
	content = sr.ReadToEnd();
	sr.Close();
}

content = content.Replace("[/", "</t").Replace("[", "<t").Replace("]", ">");

using (StreamWriter sw = new(Path.Combine(root, pattern_conf + ".xml")))
{
	sw.Write(content);
	sw.Close();
}

Console.WriteLine("Закончена запись файла!");
#endif

#if false
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

const string filename = @"C:\Users\grinm\Documents\_Job_\_MapManager_\Programs\MapMan\UserData\Pattern.conf.xml";

PatternSettings pattern = new ConfReader().LoadSettings("{\"DrawType\": \"Polyline\", \"PenColor\": 0, \"BrushColor\": 16777042, \"BrushBkColor\": 11382189, \"Width\": 1, \"Closed\": \"true\", \"BitmapName\":\"DRO32\", \"BitmapIndex\": 47, \"Transparent\": \"true\", \"nPenStyle\": 0}");

XDocument doc = XDocument.Load(filename);

var root = doc.Element("AcadPatterns");

var patternDescriptionElement = root.Element(pattern.name).Element($"t{pattern.index}");

string value = patternDescriptionElement.Value.Trim();
Console.WriteLine(value.Split('=')[1]);

Console.WriteLine("Закончена обработка xml файла!");

public struct PatternSettings
{
    public readonly string name;
	public readonly int index;
    public PatternSettings(string name, int index)
    {
        this.name = name;
		this.index = index;
    }
}

public class ConfReader
{
	public PatternSettings LoadSettings(string json)
	{
		JObject obj = JObject.Parse(json);
		string name = obj.Value<string>("BitmapName");
		int index = obj.Value<int>("BitmapIndex");

		return new PatternSettings(name, index);
	}
}
#endif