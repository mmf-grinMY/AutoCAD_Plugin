using Newtonsoft.Json.Linq;

namespace Plugins
{
    public class DrawParameters
    {
        public JObject DrawSettings { get; set; }
        public string WKT { get; set; }
        public JObject Param { get; set; }
        public string SubleyerGUID { get; set; }
        public string LayerName { get; set; }
        public string SublayerName { get; set; }
        public bool IsSimeSublayers { get; set; }
        public override string ToString()
        {
            return $"{DrawSettings};{WKT}";
        }
    }
}