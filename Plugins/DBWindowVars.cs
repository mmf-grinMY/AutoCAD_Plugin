namespace Plugins
{
    internal class DBWindowVars : WindowVars
    {
        public DBWindowVars(string username,
                            string password,
                            string host,
                            string privilege,
                            string transactionTableName,
                            string layersTableName)
        {
            Username = username;
            Password = password;
            Host = host;
            Privilege = privilege;
            TransactionTableName = transactionTableName;
            LayersTableName = layersTableName;
        }
        public string Username { get; }
        public string Password { get; }
        public string Host { get; }
        public string Privilege { get; }
        public string TransactionTableName { get; }
        public string LayersTableName { get; }
    }
    internal abstract class WindowVars { }
}
