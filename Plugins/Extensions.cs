using System.Net.NetworkInformation;
using System.Text;
using System.Collections.Generic;

namespace Plugins
{
    public static class Extensions
    {
        public static string MyToString(this string[] str)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("[");
            for (int i = 0; i < str.Length - 1; i++)
            {
                builder.Append(str[i]);
                builder.Append(", ");
            }
            builder.Append(str[str.Length - 1]);
            builder.Append("]");
            return builder.ToString();
        }
    }

    public static class ListTemplateExtension
    {
        public static string ToString<T>(this List<T> list)
        {
            if (list.Count == 0)
                return string.Empty;
            StringBuilder builder = new StringBuilder();
            builder.Append("[\n");
            for (int i = 0; i < list.Count - 1; i++)
            {
                builder.Append($"\t{list[i].ToString().TrimEnd(' ')},\n");
            }
            int lastIndex = list.Count - 1;
            builder.Append($"\t{list[lastIndex]}\n]");
            return builder.ToString();
        }
    }
}