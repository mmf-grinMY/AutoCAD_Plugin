namespace Plugins.WKT.Old
{
    public class MultiLineStrings : MultiLine
    {
        public MultiLineStrings(string source) : base(source) { }
        public override string ToString()
        {
            return "MULTILINESTRING" + base.ToString();
        }
    }
}