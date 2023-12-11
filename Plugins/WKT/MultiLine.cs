#region Usings

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.Text;
using System.Linq;
using System.Windows;

using static Plugins.WKT.Old.RegExp;

#endregion

namespace Plugins.WKT.Old
{
    public class MultiLine
    {
        private readonly List<LineString> _lines = new List<LineString>();
        public List<LineString> Lines => _lines;
        public MultiLine(string source)
        {
            try
            {
                MatchCollection lines = line.Matches(source);
                if (lines.Count == 0) throw new Exception();
                foreach (Match line in lines)
                {
                    _lines.Add(new LineString(line.Value));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message} {source}");
            }
        }
        public override string ToString()
        {
            if (Lines.Count == 0) return string.Empty;
            StringBuilder builder = new StringBuilder();
            builder.Append("(");
            for (int i = 0; i < Lines.Count - 1; i++)
                builder.Append($"{Lines[i]},");
            return builder.Append($"{Lines.Last<LineString>()})").ToString();
        }
    }
}