using Plugins.Logging;

using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows;
using System.Text;
using System.IO;
using System;

using Oracle.ManagedDataAccess.Client;
using Plugins.Entities;
using Plugins.View;

namespace Plugins
{
    class OracleDbDispatcher : IDisposable
    {
        #region Private Fields

        /// <summary>
        /// Логер событий
        /// </summary>
        readonly ILogger logger;
        /// <summary>
        /// Подключение к Oracle БД
        /// </summary>
        readonly OracleConnection connection;
        /// <summary>
        /// Текущий горизонт
        /// </summary>
        readonly string gorizont;

        #endregion

        #region Private Properties

        /// <summary>
        /// Параметры подключения
        /// </summary>
        /// <exception cref="TypeInitializationException"></exception>
        static ConnectionParams ConnectionParams
        {
            get
            {
                using (var loginWindow = new View.LoginWindow())
                {
                    loginWindow.ShowDialog();

                    if (!loginWindow.InputResult)
                    {
                        throw new TypeInitializationException(nameof(ConnectionParams), new ArgumentNullException());
                    }

                    return loginWindow.Params;
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Преобразование json-строки в удобочитаемый вид
        /// </summary>
        /// <param name="json">исходная строка в формате json</param>
        /// <returns>Преобразованная строка</returns>
        string JsonNormalize(string json) => json.Substring(1, json.Length - 1).Replace(',', '\n');

        #endregion

        #region Ctors

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="log">Логер событий</param>
        /// <exception cref="TypeInitializationException"></exception>
        public OracleDbDispatcher(ILogger log)
        {
            logger = log ?? throw new ArgumentNullException(nameof(log));

            connect:
            try
            {
                connection = new OracleConnection(GetDbConnectionStr(ConnectionParams));
                connection.Open();

                if (gorizont is null)
                {
                    var gorizontSelecter = new GorizontSelecterWindow(Gorizonts);
                    gorizontSelecter.ShowDialog();

                    if (!gorizontSelecter.InputResult)
                    {
                        throw new ArgumentException("Не удалось получить рисуемый горизонт!");
                    }

                    gorizont = gorizontSelecter.Gorizont;
                    gorizontSelecter.Close();
                }
            }
            catch (OracleException ex)
            {
                if (ex.Number == 12154)
                {
                    if (MessageBox.Show("Неправильно указаны данные подключения к базе данных!\n\nЖелаете еще раз попробовать подключиться к базе данных?", "Ошибка подключения", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                    {
                        goto connect;
                    }
                }
                else if (ex.Number == 1017)
                {
                    MessageBox.Show("Неправильный логин или пароль!");
                    goto connect;
                }
                else
                {
                    MessageBox.Show("При попытки подключения произошла ошибка! Перепроверьте параметры покдлюченя и повторите попытку!");
                    goto connect;
                }
            }
            catch (TypeInitializationException)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e);
                goto connect;
            }
        }
#if DEBUG
        public OracleDbDispatcher(string connectionStr, string gorizont)
        {
            if (string.IsNullOrWhiteSpace(gorizont))
                throw new ArgumentException(nameof(gorizont));

            this.gorizont = gorizont;
            connection = new OracleConnection(connectionStr);
            connection.Open();
        }
#endif

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Получение строки подключения из параметров подключения
        /// </summary>
        /// <param name="param">Параметры подключения</param>
        /// <returns>Строка подключения</returns>
        public static string GetDbConnectionStr(ConnectionParams param)
            => new StringBuilder()
                .Append("Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)(HOST = ")
                .Append(param.Host)
                .Append(")(PORT = ")
                .Append(param.Port)
                .Append("))(CONNECT_DATA = (SERVER = DEDICATED)(SERVICE_NAME = ")
                .Append(param.Sid)
                .Append(")));Password=")
                .Append(param.Password)
                .Append(";User ID=")
                .Append(param.UserName)
                .Append(";Connection Timeout = 360;")
                .ToString();
        /// <summary>
        /// Установка соединения с БД
        /// </summary>
        /// <param name="connection">Менеджер соединения</param>
        /// <param name="logger">Логер событий</param>
        /// <returns>true, если удалось установить соединение, false в противном случае</returns>
        public static bool TryGetConnection(ILogger logger, out OracleDbDispatcher connection)
        {
            try
            {
                connection = new OracleDbDispatcher(logger);
                return true;
            }
            catch (TypeInitializationException)
            {
                connection = null;
                return false;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Доступные для отрисовки горизонты
        /// </summary>
        public ObservableCollection<string> Gorizonts
        {
            get
            {
                var gorizonts = new ObservableCollection<string>();
                var selectedGorizonts = new Dictionary<string, bool>();
                const string command = "SELECT table_name FROM all_tables " +
                    "WHERE table_name LIKE 'K%_TRANS_CLONE' OR table_name LIKE 'K%_TRANS_OPEN_SUBLAYERS'";

                using (var reader = new OracleCommand(command, connection).ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string tableName = reader.GetString(0).Split('_')[0];

                        if (selectedGorizonts.ContainsKey(tableName))
                        {
                            selectedGorizonts[tableName] = true;
                        }
                        else
                        {
                            selectedGorizonts.Add(tableName, false);
                        }
                    }
                }

                foreach (var key in selectedGorizonts.Keys)
                    if (selectedGorizonts[key])
                        gorizonts.Add(key);

                return gorizonts;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Освобождение занятых ресурсов
        /// </summary>
        public void Dispose() => connection.Dispose();
        /// <summary>
        /// Получение параметров отрисовки
        /// </summary>
        /// <param name="gorizont">Выбранный горизонт</param>
        /// <param name="position">Текщуая позиция читателя БД</param>
        /// <returns>Читатель данных</returns>
        public OracleDataReader GetDrawParams(uint position, Session session)
        {
            string command;

            using (var reader = new StreamReader(Path.Combine(Constants.AssemblyPath, "draw.sql")))
            {
                command = string.Format(reader.ReadToEnd(), gorizont, position, session.Right, session.Left, session.Top, session.Bottom);
            }

            return new OracleCommand(command, connection).ExecuteReader();
        }
        /// <summary>
        /// Количество записей на горизонте, доступных для отрисовки
        /// </summary>
        /// <param name="gorizont">Имя горизонта для поиска</param>
        /// <returns>Количество записей</returns>
        public int Count 
        {
            get
            {
                string command = "SELECT COUNT(*) FROM " + gorizont + "_trans_clone";

                using (var reader = new OracleCommand(command, connection).ExecuteReader())
                {
                    reader.Read();
                    return reader.GetInt32(0);
                }
            }
        }
        /// <summary>
        /// Получение линковки
        /// </summary>
        /// <param name="baseName">Имя таблицы для линковки</param>
        /// <returns></returns>
        public string GetExternalDbLink(string baseName)
        {
            string command = "SELECT data FROM LINKS WHERE NAME = '" + baseName + "'";

            using (var reader = new OracleCommand(command, connection).ExecuteReader())
            {
                if (reader.Read()) return reader.GetString(0);
            }

            return string.Empty;
        }
        /// <summary>
        /// Получение таблицы данных
        /// </summary>
        /// <param name="command">Команда для получения данных</param>
        /// <returns>Таблица данных</returns>
        public System.Data.DataTable GetDataTable(string command)
        {
            using (var reader = new OracleCommand(command, connection).ExecuteReader())
            {
                var dataTable = new System.Data.DataTable();
                dataTable.Load(reader);

                return dataTable;
            }
        }
        /// <summary>
        /// Получение характеристик отрисвоки объекта по его Id
        /// </summary>
        /// <param name="id">Id объекта в БД Oracle</param>
        /// <returns>Строковое представление характеристик отрисовки</returns>
        public string GetObjectJsonById(int id)
        {
            var command = string.Format("SELECT * FROM (SELECT a.drawjson, a.paramjson, ROWNUM AS rn FROM {0}_trans_clone a " + 
                "INNER JOIN {0}_trans_open_sublayers b ON a.sublayerguid = b.sublayerguid WHERE a.geowkt IS NOT NULL) WHERE rn = {1}", gorizont, id);

            using (var reader = new OracleCommand(command, connection).ExecuteReader())
            {
                if (reader.Read())
                {
                    return JsonNormalize(reader["drawjson"].ToString()) + "\n\n" + JsonNormalize(reader["paramjson"].ToString());
                }
            }

            return string.Empty;
        }
        /// <summary>
        /// Получение геометрии больших объектов
        /// </summary>
        /// <param name="primitive">Исходный примитив</param>
        /// <returns>Геометрия в формате wkt</returns>
        public string GetLongGeometry(Primitive primitive)
        {
            var command = "SELECT page FROM {gorizont}_trans_clone_geowkt WHERE objectguid = '" + 
                primitive.Guid.ToString().ToUpper() + " ORDER BY numb";
            var builder = new StringBuilder().Append(primitive.Geometry);

            using (var reader = new OracleCommand(command, connection).ExecuteReader())
            {
                while (reader.Read())
                {
                    builder.Append(reader.GetString(0));
                }
            }

            return builder.ToString();
        }

        #endregion
    }
}