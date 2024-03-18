using System.Text;
using System.Linq;
using System.IO;
using System;

using Newtonsoft.Json.Linq;

using static Plugins.Constants;

namespace Plugins
{
    sealed class LineTypeLoader
    {
        public static readonly string STYLE_FILE = ".tmp.lin";
        public static readonly string STYLE_NAME = "MM_LineType";
        public int Load(double scale)
        {
            var content = File.ReadAllLines(Path.Combine(Constants.SupportPath, "acad.lin"));

            var styles = JObject
                .Parse(File.ReadAllText(Path.Combine(Constants.SupportPath, "plugin.config.json")))
                .Value<JArray>("LineTypes")
                .Values<string>();

            int counter = 0;

            using (var stream = new StreamWriter(Path.Combine(SupportPath, STYLE_FILE), false))
            {
                for (int i = 0; i < content.Length; ++i)
                {
                    var line = content[i];
                    if (line.StartsWith(";;"))
                    {
                        continue;
                    }
                    else if (line.StartsWith("*"))
                    {
                        foreach (var style in styles)
                        {
                            if (line.StartsWith("*" + style))
                            {
                                stream.WriteLine("*" + STYLE_NAME + (++counter).ToString());
                                var descriptionType = content[i + 1];
                                var numbers = descriptionType.Substring(2, descriptionType.Length - 2).Split(',');
                                var builder = new StringBuilder().Append('A');
                                foreach(var number in numbers)
                                {
                                    builder.Append(',').Append(Convert.ToDouble(number.StartsWith(".") ? "0" + number : number) * scale);
                                }
                                stream.WriteLine(builder.ToString());
                                ++i;
                            }
                        }
                    }
                }
            }

            if (counter == styles.Count())
                return counter;
            else
                throw new LineTypeNotFoundException();
        }
    }
}