using Plugins.Entities;
using Plugins.Logging;
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
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Plugins
{
    /// <summary>
    ///  Диспетчер для работы с БД Oracle
    /// </summary>
    class OracleDbDispatcher : IDbDispatcher
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
        public string GetExternalDbLink(string baseName)
        {
            string command = "SELECT data FROM LINKS WHERE NAME = '" + baseName + "'";

            using (var reader = new OracleCommand(command, connection).ExecuteReader())
            {
                if (reader.Read()) return reader.GetString(0);
            }

            return string.Empty;
        }
        public System.Data.DataTable GetDataTable(string command)
        {
            using (var reader = new OracleCommand(command, connection).ExecuteReader())
            {
                var dataTable = new System.Data.DataTable();
                dataTable.Load(reader);

                return dataTable;
            }
        }
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
        public async void ReadAsync(CancellationToken token, ConcurrentQueue<Primitive> queue, DrawInfoViewModel model, Session session)
        {
            await Task.Run(async () =>
            {
                var readPosition = model.readPosition;
                var totalCount = Count;
                var percent = readPosition * 100 / totalCount;
                var command = string.Empty;

                using (var stream = new StreamReader(Path.Combine(Constants.AssemblyPath, "draw.sql")))
                {
                    command = string.Format(stream.ReadToEnd(), gorizont, readPosition,
                        session.Right, session.Left, session.Top, session.Bottom);
                }

                OracleDataReader reader = null;

                try
                { 
                    reader = new OracleCommand(command, connection).ExecuteReader();
                    while (reader.Read())
                    {
                        if (token.IsCancellationRequested) return;

                        while (queue.Count > Constants.QueueLimit) await Task.Delay(Constants.ReaderSleepTime);

                        queue.Enqueue(new Primitive(reader["geowkt"].ToString(),
                                                          reader["drawjson"].ToString(),
                                                          reader["paramjson"].ToString(),
                                                          reader["layername"] + " _ " + reader["sublayername"],
                                                          reader["systemid"].ToString(),
                                                          reader["basename"].ToString(),
                                                          reader["childfields"].ToString(),
                                                          reader["rn"].ToString(),
                                                          reader["objectguid"].ToString()));

                        var currentPrecent = ++readPosition * 100 / totalCount;

                        if (currentPrecent > percent)
                        {
                            percent = currentPrecent;
                            model.ReadProgress = percent;
                        }
                    }
                }
                // Вызывается из-за отсутсвия некоторых запрашиваемых столбцов в таблице
                catch (OracleException e)
                {
                    var logger = new FileLogger(Path.Combine(Constants.AssemblyPath, "Logs", nameof(OracleDbDispatcher) + ".log"));
                    logger.LogError(e);
                }
                finally
                {
                    model.readPosition = readPosition;
                    model.isReadEnded = true;
                    reader?.Dispose();
                }
            }, token);
        }
        public IEnumerable<string> GetLayers()
        {
            string command = "SELECT layername, sublayername FROM ( " + 
                $"SELECT DISTINCT b.layername, b.sublayername FROM k{gorizont}_trans_clone a" + 
                $" INNER JOIN k{gorizont}_trans_open_sublayers b ON a.sublayerguid = b.sublayerguid)";

            using (var reader = new OracleCommand(command, connection).ExecuteReader())
            {
                while (reader.Read())
                {
                    yield return Regex.Replace(reader.GetString(0) + " _ " + reader.GetString(1), "[<>\\*\\?/|\\\\\":;,=]", "_");
                }
            }
        }

        #endregion
    }
}