namespace Plugins
{
    public class DrawParameters
    {
        public DrawSettings DrawSettings { get; set; }
        public string WKT { get; set; }
        public string SubleyerGUID { get; set; }
        public override string ToString()
        {
            return $"{DrawSettings};{WKT}";
        }
    }
}