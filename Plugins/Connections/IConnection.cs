using System;
using System.Xml;

namespace Plugins
{
    internal interface IConnection : IDisposable
    {
        XmlElement Connect();
        void Close();
    }
}