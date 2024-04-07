using Plugins.Entities;
using Plugins.View;

using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Text;
using System.IO;
using System;

using Oracle.ManagedDataAccess.Client;

namespace Plugins
{
    class OracleDbDispatcher : IDisposable
    {
        #region Private Fields

        /// <summary>
        /// Подключение к Oracle БД
        /// </summary>
        readonly OracleConnection connection;
        /// <summary>
        /// Текущий горизонт
        /// </summary>
        readonly string gorizont;

        #endregion

        #region Ctor

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="connectionStr">Строка подключения</param>
        /// <param name="gorizont">Выбранный горизонт</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public OracleDbDispatcher(string connectionStr = null, string gorizont = null)
        {
            var isCreated = false;

            while (!isCreated)
            {
                try
                {
                    connection?.Dispose();
                    (connection = new OracleConnection(connectionStr)).Open();
                    isCreated = true;
                }
                catch (OracleException e)
                {
                    MessageBox.Show(e.GetCodeDescription(), "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error);
                    connectionStr = DbHelper.ConnectionStr;
                }
                catch (InvalidOperationException)
                {
                    connectionStr = DbHelper.ConnectionStr;
                }
            }

            this.gorizont = gorizont ?? DbHelper.SelectGorizont(Gorizonts);
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
                const string command =
"SELECT DISTINCT SUBSTR(table_name, 2, INSTR(table_name, '_', 2) - 2) AS pattern FROM all_tables " + 
"WHERE table_name LIKE 'K%_TRANS_CLONE' AND SUBSTR(table_name, 2, INSTR(table_name, '_', 2) - 2) IN (" + 
"SELECT SUBSTR(table_name, 2, INSTR(table_name, '_', 2) - 2) FROM all_tables WHERE table_name LIKE 'K%_TRANS_OPEN_SUBLAYERS')";

                using (var reader = new OracleCommand(command, connection).ExecuteReader())
                {
                    while (reader.Read())
                    {
                        gorizonts.Add(reader.GetString(0));
                    }
                }

                return gorizonts;
            }
        }
        /// <summary>
        /// Количество записей на горизонте, доступных для отрисовки
        /// </summary>
        /// <param name="gorizont">Имя горизонта для поиска</param>
        /// <returns>Количество записей</returns>
        public uint Count
        {
            get
            {
                string command = "SELECT COUNT(*) FROM k" + gorizont + "_trans_clone";

                using (var reader = new OracleCommand(command, connection).ExecuteReader())
                {
                    reader.Read();
                    return Convert.ToUInt32(reader.GetInt32(0));
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Освобождение занятых ресурсов
        /// </summary>
        public void Dispose() => connection?.Dispose();
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
            var command = string.Format("SELECT * FROM (SELECT a.drawjson, a.paramjson, ROWNUM AS rn FROM k{0}_trans_clone a " + 
                "INNER JOIN k{0}_trans_open_sublayers b ON a.sublayerguid = b.sublayerguid WHERE a.geowkt IS NOT NULL) WHERE rn = {1}", gorizont, id);

            using (var reader = new OracleCommand(command, connection).ExecuteReader())
            {
                if (reader.Read())
                {
                    return reader["drawjson"].ToString().JsonNormalize() + "\n\n" + reader["paramjson"].ToString().JsonNormalize();
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
            var command = $"SELECT page FROM k{gorizont}_trans_clone_geowkt WHERE objectguid = '" + 
                primitive.Guid.ToString().ToUpper() + "' ORDER BY numb";
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

        public async void ReadAsync(CancellationToken token, ConcurrentQueue<Primitive> queue, DrawInfoViewModel model, Session session)
        {
            uint readPosition = model.readPosition;
            uint totalCount = Count;

            await Task.Run(async () =>
            {
                uint percent = readPosition * 100 / totalCount;

                string command;

                using (var stream = new StreamReader(Path.Combine(Constants.AssemblyPath, "draw.sql")))
                {
                    command = string.Format(stream.ReadToEnd(), gorizont, readPosition, session.Right, session.Left, session.Top, session.Bottom);
                }

                using (var reader = new OracleCommand(command, connection).ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (token.IsCancellationRequested) return;

                        while (queue.Count > Constants.QueueLimit) await Task.Delay(Constants.ReaderSleepTime);

                        queue.Enqueue(new Primitive(reader["geowkt"].ToString(),
                                                          reader["drawjson"].ToString(),
                                                          reader["paramjson"].ToString(),
                                                          reader["layername"] + " | " + reader["sublayername"],
                                                          reader["systemid"].ToString(),
                                                          reader["basename"].ToString(),
                                                          reader["childfields"].ToString(),
                                                          reader["rn"].ToString(),
                                                          reader["objectguid"].ToString()));

                        uint currentPrecent = ++readPosition * 100 / totalCount;

                        if (currentPrecent > percent)
                        {
                            percent = currentPrecent;
                            model.ReadProgress = percent;
                        }
                    }

                    model.readPosition = readPosition;
                    model.isReadEnded = true;
                }
            }, token);
        }
    }
    static class DbHelper
    {
        static string GetResult<T>(T window) where T : Window, IResult
        {
            string result = null;

            window.ShowDialog();

            if (window.IsSuccess)
            {
                result = window.Result;
            }

            window.Close();

            return result;
        }
        public static string ConnectionStr => GetResult(new LoginWindow());
        public static string SelectGorizont(ObservableCollection<string> gorizonts) => GetResult(new GorizontSelecterWindow(gorizonts));
    }
}