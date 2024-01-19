// #define CONVERTER // Конвертер из CHR в BDF
#define DEF_READER // Чтение файла с шаблоном линий

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
DefConverter.ToJson(filePath, filePath + ".json");
Console.WriteLine("Reader закончил свою работу!");

#endif

Console.Read();