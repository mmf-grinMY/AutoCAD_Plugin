﻿using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Text;
using System;

using Oracle.ManagedDataAccess.Client;
using System.Data.Common;

namespace Plugins
{
    // TODO: Сделать по возможности все SQL-запросы читаемыми извне
    public class OracleDbDispatcher : IDisposable
    {
        #region Private Fields
        /// <summary>
        /// Подключение к Oracle БД
        /// </summary>
        private readonly OracleConnection connection;
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
        /// <returns>true, если удалось установить соединение, false в противном случае</returns>
        public static bool TryGetConnection(out OracleDbDispatcher connection)
        {
            try
            {
                connection = new OracleDbDispatcher();
                return true;
            }
            catch (CtorException)
            {
                connection = null;
                return false;
            }
        }
        private static ConnectionParams ConnectionParams
        {
            get
            {
                using (var loginWindow = new View.LoginWindow())
                {
                    loginWindow.ShowDialog();

                    if (!loginWindow.InputResult)
                    {
                        throw new CtorException();
                    }

                    return loginWindow.Params;
                }
            }
        }

        #endregion

        #region Ctors

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="connectionStr">Строка подключения к Oracle БД</param>
        public OracleDbDispatcher() 
        {
connect:
            try
            {
                connection = new OracleConnection(GetDbConnectionStr(ConnectionParams));
                connection.Open();
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
            catch (CtorException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
                goto connect;
            }
        }
#if DEBUG
        public OracleDbDispatcher(string connectionStr)
        {
            connection = new OracleConnection(connectionStr);
            connection.Open();
        }
#endif

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
                {
                    if (selectedGorizonts[key])
                    {
                        gorizonts.Add(key);
                    }
                }
                return gorizonts;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Освобождение занятых ресурсов
        /// </summary>
        public void Dispose() 
        {
            connection.Dispose();
        }
        /// <summary>
        /// Получение параметров отрисовки
        /// </summary>
        /// <param name="gorizont">Выбранный горизонт</param>
        /// <returns>Читатель данных</returns>
        public OracleDataReader GetDrawParams(string gorizont)
        {
            string command =
                "SELECT * FROM ( SELECT * FROM " + gorizont + "_trans_clone a JOIN " + gorizont +
                "_trans_open_sublayers b ON a.sublayerguid = b.sublayerguid WHERE geowkt IS NOT NULL)";

            return new OracleCommand(command, connection).ExecuteReader();
        }
        public int Count(string gorizont)
        {
            string command = "SELECT COUNT(*) FROM " + gorizont + "_trans_clone";
            var reader = new OracleCommand(command, connection).ExecuteReader();
            reader.Read();
            return reader.GetInt32(0);
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
        #endregion
    }
    public sealed class CtorException : Exception { }
}