namespace Plugins
{
    class SqlQuery
    {
        private string query;
        public override string ToString()
        {
            return query;
        }
        public SqlQuery()
        {
            query = string.Empty;
        }
        public SqlQuery Select(string args, string tableName)
        {
            query = "SELECT " + args + " FROM " + tableName;
            return this;
        }
        public SqlQuery Count(string tableName)
        {
            query = "SELECT count(*) FROM " + tableName;
            return this;
        }
    }
}