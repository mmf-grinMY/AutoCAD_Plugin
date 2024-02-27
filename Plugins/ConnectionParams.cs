using Newtonsoft.Json;

namespace Plugins
{
    /// <summary>
    /// Параметры подключения
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ConnectionParams
    {
        /// <summary>
        /// Пароль
        /// </summary>
        public string Password { get; }
        /// <summary>
        /// Имя пользователя
        /// </summary>
        [JsonProperty]
        public string UserName { get; }
        /// <summary>
        /// Имя хоста
        /// </summary>
        [JsonProperty]
        public string Host { get; }
        /// <summary>
        /// Номер порта
        /// </summary>
        [JsonProperty]
        public int Port { get; }
        /// <summary>
        /// Имя БД
        /// </summary>
        [JsonProperty]
        public string Sid { get; }
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="username">Имя пользователя</param>
        /// <param name="pass">Пароль</param>
        /// <param name="host">Имя хоста</param>
        /// <param name="port">Номер порта</param>
        /// <param name="sid">Имя БД</param>
        public ConnectionParams(string username, string pass, string host, int port, string sid)
        {
            UserName = username;
            Password = pass;
            Host = host;
            Port = port;
            Sid = sid;
        }
    }
}