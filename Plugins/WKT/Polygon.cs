namespace Plugins.WKT.Old
{
    public class Polygon : MultiLine
    {
        public Polygon(string source) : base(source) { }
        public override string ToString()
        {
            string basedString = base.ToString();
            return "POLYGON" + base.ToString();
        }
    }
}