using System;
using System.Windows;
using System.Xml;

namespace Plugins
{
    public class XmlConnection : IConnection
    {
        private XmlDocument _doc;
        private readonly string _connectionString;
        public string ConnectionString => _connectionString;
        public XmlConnection(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            _connectionString = connectionString;
        }
        public void Close()
        {
            _doc = null;
        }

        public XmlElement Connect()
        {
            _doc = new XmlDocument();
            try
            {
                _doc.Load(ConnectionString);
            }
            catch
            {
                MessageBox.Show("Не удалось открыть файл \"" + ConnectionString + "\"!");
            }
            return _doc.DocumentElement;
        }

        public void Dispose()
        {
            Close();
        }
    }
}