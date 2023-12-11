namespace Plugins
{
    internal class XmlWindowVars : WindowVars
    {
        public XmlWindowVars(string geometryPath, string layersPath)
        {
            GeometryPath = geometryPath;
            LayersPath = layersPath;
        }
        public string GeometryPath { get; }
        public string LayersPath { get; }
    }
}
