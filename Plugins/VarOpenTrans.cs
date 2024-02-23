using System;
using System.Windows;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Oracle.ManagedDataAccess.Client;

using Plugins.View;

namespace Plugins
{
    // TODO: Сделать рефакторинг кода
    public class ConnectionParams
    {
        public string UserName { get; }
        public string Password { get; }
        public string Host { get; }
        public int Port { get; }
        public string Sid { get; }
        public ConnectionParams(string name, string pass, string host, int port, string sid)
        {
            UserName = name;
            Password = pass;
            Host = host;
            Port = port;
            Sid = sid;
        }
    }
    class VarOpenTrans
    {
        public string sufTransOpen = "_TRANS_OPEN";
        public string sufTransClone = "_TRANS_CLONE";
        public string sufTransControl = "_TRANS_CONTROL";
        public string sufTransSubLayers = "_TRANS_OPEN_SUBLAYERS";

        public string host;
        public int port;
        public string sid;

        public Dictionary<string, string> fieldNames;
        public OracleConnection dbcon = null;
        public VarOpenTrans()
        {
            fieldNames = new Dictionary<string, string>();
        }
//        public void InitConnectionParams()
//        {
//#if OLD
//            using (var reader = new System.IO.StreamReader(System.IO.Path.Combine(Constants.SupportPath, "db.config"))) 
//            {
//                string content = reader.ReadToEnd();
//                var obj = JsonDocument.Parse(content).RootElement;
//                host = obj.GetProperty("host").GetString();
//                port = obj.GetProperty("port").GetInt32();
//                sid = obj.GetProperty("sid").GetString();
//            }
//#else
//#endif
//        }
        public bool TryConnectAndSave(ConnectionParams param)
        {
            dbcon = GetDBConnection(param);
            try
            {
                dbcon.Open();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
            return true;
        }
        public static OracleConnection GetDBConnection(ConnectionParams param)
        {
            string connString = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)(HOST = "
                    + param.Host + ")(PORT = " + param.Port + "))(CONNECT_DATA = (SERVER = DEDICATED)(SERVICE_NAME = "
                    + param.Sid + ")));Password=" + param.Password + ";User ID=" + param.UserName + ";Connection Timeout = 360;";

            return new OracleConnection { ConnectionString = connString };
        }
        public bool TryGetPassword(out ConnectionParams connectionParams)
        {
#if OLD
            SimpleLoginWindow window = new SimpleLoginWindow();
            if (window.ShowDialog() == false)
            {
                username = password = string.Empty;
                return false;
            }
            username = window.LoginDto.Username;
            password = window.LoginDto.Password;
#else
            var loginWindow = new LoginWindow();
            loginWindow.ShowDialog();
            if (!loginWindow.InputResult)
            {
                loginWindow.Close();
                connectionParams = null;
                return false;
            }
            connectionParams = loginWindow.Params;
            loginWindow.Close();
#endif
            return true;
        }
        public bool ParseExternalDbLink(string BaseName, out string table_data_fin, OracleConnection dbcon)
        {
            table_data_fin = "Empty";
            fieldNames.Clear();
            try
            {
                if (true)
                {
                    string command_str = "SELECT * FROM LINKS WHERE NAME = '" + BaseName + "'";
                    OracleCommand command = new OracleCommand(command_str, dbcon);
                    try
                    {
                        OracleDataReader dr = command.ExecuteReader();
                        while (dr.Read())
                        {
                            table_data_fin = dr["DATA"].ToString();
                        }
                        dr.Close();
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return false;
                    }
                }
                string[] f = table_data_fin.Split('\n');
                if (f.Count() > 5)
                {
                    bool fields_fl = true;
                    for (int i = 0; i < f.Count(); i++)
                    {
                        if (fields_fl)
                        {
                            if (f[i] == "FIELDS")
                            {
                                fields_fl = false;
                            }
                            continue;
                        }
                        if (f[i] == "ENDFIELDS")
                        {
                            break;
                        }
                        if (f[i].Contains("+"))
                            continue;
                        var row_0 = f[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (row_0.Count() > 1)
                        {
                            string field_value = "";
                            for (int j = 1; j < row_0.Count(); j++)
                            {
                                field_value += row_0[j] + "_";
                            }

                            if (!fieldNames.ContainsKey(row_0[0]))
                            {
                                fieldNames.Add(row_0[0], field_value);
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return true;
        }
        public bool GetExternalDb(string BaseName, string BaseCaption, string ChildField, int SystemID)
        {
            try
            {
                if (true)
                {
                    string command_str = "SELECT ";
                    int field_count = 0;
                    foreach (var item in fieldNames)
                    {
                        if (field_count > 0)
                            command_str += " , ";
                        command_str += item.Key + " as \"" + item.Value + "\"";
                        field_count++;
                    }
                    command_str += " FROM " + BaseName + " WHERE " + ChildField + " = " + SystemID.ToString();
                    try
                    {
                        using (var reader = new OracleCommand(command_str, dbcon).ExecuteReader())
                        {
                            System.Data.DataTable dataTable = new System.Data.DataTable();
                            dataTable.Load(reader);
                            using (ExternalDBWindow window = new ExternalDBWindow(dataTable))
                            {
                                window.Title = BaseCaption;
                                window.ShowDialog();
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return false;
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return true;
        }
        public void Close()
        {
            dbcon.Close();
        }
    }
}