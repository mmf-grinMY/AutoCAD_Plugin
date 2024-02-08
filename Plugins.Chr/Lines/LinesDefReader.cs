using System.Text;
using System.Text.RegularExpressions;

namespace Plugins.Chr.Lines;
public partial class DefConverter
{
    public static void ToJson(string path, string output)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var reader = new StreamReader(path, Encoding.GetEncoding("windows-1251"));
        string content = reader.ReadToEnd();
        reader.Close();

        using var writer = new StreamWriter(output);
        Stack<string> tags = new();
        string[] lines = content.Split('\n').Select(s => s.Trim()).ToArray();
        Regex exp = Expression();
        bool IsPrevProp = false;
        const string comma = ",\r\n";

        writer.WriteLine("{");

        for (int i = 0; i < lines.Length; ++i)
        {
            string line = lines[i];
            if (line.StartsWith("[/"))
            {
                if (tags.First().Equals(line.Replace("[/", "[")))
                {
                    if (!IsPrevProp)
                        IsPrevProp = true;
                    writer.Write("}");
                    tags.Pop();
                }
                else
                {
                    throw new Exception("Неправильная структура файла!");
                }
            }
            else if (line.StartsWith('['))
            {
                if (IsPrevProp)
                {
                    IsPrevProp = false;
                    writer.Write(comma);
                }
                if (tags.Count > 1)
                {
                    writer.Write($"{{ \"name\" : \"{line[1..^1]}\"\r\n");
                }
                tags.Push(line);
            }
            else if (line.StartsWith(';'))
            {
                continue;
            }
            else
            {
                if (!Expression().IsMatch(line))
                {
                    throw new ArgumentException("Не удалось распознать операцию!");
                }
                else
                {
                    if (line != string.Empty)
                    {
                        var args = line.Split("=");
                        if (IsPrevProp)
                            writer.Write(comma);
                        else
                            IsPrevProp = true;
                        writer.Write($"\"{args[0].Trim()}\" : \"{args[1].Trim()}\"");
                    }
                }
            }
        }

        writer.WriteLine("}");

        writer.Close();
    }

    [GeneratedRegex("\\[(\\w+)\\]\r\n.*\\[/\\1\\]\r\n")]
    private static partial Regex Group();
    [GeneratedRegex(" *\\w+ = [- \\{\\}\\w\\,\\.]+|")]
    private static partial Regex Expression();
}
