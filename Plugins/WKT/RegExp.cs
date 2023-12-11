using System.Text.RegularExpressions;

namespace Plugins.WKT.Old
{
    public static class RegExp
    {
        public static readonly Regex line = new Regex(@"\(([+-]?\d+(\.\d{0,3})? [+-]?\d+(\.\d{0,3})?,( ?))+[+-]?\d+(\.\d{0,3})? [+-]?\d+(\.\d{0,3})?\)");
        public static readonly Regex point = new Regex(@"[+-]?\d+(\.\d{0,3})? [+-]?\d+(\.\d{0,3})?");
    }
}