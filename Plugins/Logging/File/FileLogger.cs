namespace Plugins.Logging
{
    sealed class FileLogger : ILogger
    {
        static readonly object lockObj = new object();
        readonly string filePath;
        public FileLogger(string path)
        {
            filePath = path;
        }
        public void Log(LogLevel level, string message, System.Exception ex)
        {
            lock (lockObj)
            {
                bool isEmpty = true;
                var builder = new System.Text.StringBuilder();
                if (message != null && message != string.Empty)
                {
                    builder
                        .Append(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append(" | ").Append(level).AppendLine(":")
                        .Append('\t').AppendLine(message);

                    isEmpty = false;
                }

                if (ex != null)
                {
                    if (isEmpty) 
                        builder.Append(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append(" | ").Append(level).AppendLine(":");

                    builder
                        .Append('\t').AppendLine(ex.Message)
                        .Append('\t').AppendLine(ex.Source)
                        .Append('\t').AppendLine(ex.StackTrace);

                    isEmpty = false;
                }

                if (!isEmpty)
                {
                    System.IO.File.AppendAllText(filePath, builder.ToString());
                }
            }
        }
    }
}