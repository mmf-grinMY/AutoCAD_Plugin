using Plugins.Chr;

const string chrFilePath = @"C:\Users\grinm\Documents\_Job_\_MapManager_\Programs\MapMan\Fonts\pnt!.chr";
var path = chrFilePath.Split('\\');
var name = path[path.Length - 1].Replace(".chr", ".new.bdf");
const string outputRoot = @"C:\Users\grinm\Desktop\_AutoCAD_\Fonts";
var font = ChrReader.Read(chrFilePath);
font.Dump(Path.Combine(outputRoot, name));
Console.WriteLine("Закончена запись шрифта!");
Console.Read();