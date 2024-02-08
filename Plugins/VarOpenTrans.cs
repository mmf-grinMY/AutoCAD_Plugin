using System;
using System.Windows;
using System.Linq;
using System.Collections.Generic;

using Oracle.ManagedDataAccess.Client;

using Plugins.View;

namespace Plugins
{
    // TODO: Сделать рефакторинг кода
    public class SubLayerGuid
    {
        public string SubGuid;
        public string SubName;
        public string LayerGuid;
        public string LayerName;
        public string SubLayerBaseName;
        public string SubLayerParentFiedls;
        public string SubLayerChildFields;

        public SubLayerGuid(string s1, string s2, string s3, string s4, string s5, string s6, string s7)
        {
            SubGuid = s1;
            SubName = s2;
            LayerGuid = s3;
            LayerName = s4;
            SubLayerBaseName = s5;
            SubLayerParentFiedls = s6;
            SubLayerChildFields = s7;
        }

    }
    public class OpenTransConnectionParams
    {
        public string host
        {
            get;
            set;
        }
        public int port
        {
            get;
            set;
        }
        public string sid
        {
            get;
            set;
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
        public string network_user;
        public string network_pass;
        public List<string> WktStringList;
        public List<string> DrawStringList;
        public List<string> ParamStringList;
        public List<SubLayerGuid> NameGuidList;
        public List<string> SubLayerGuidList;
        public List<string> SubLayerNameList;
        public List<string> LayerGuidList;
        public List<string> LayerNameList;
        public List<int> SystemIdList;
        public Dictionary<string, string> field_names;
        public int WktCount = 0;
        public OracleConnection dbcon = null;
        public bool InitConnectionParams()
        {
            WktStringList = new List<string>();
            DrawStringList = new List<string>();
            ParamStringList = new List<string>();
            SubLayerGuidList = new List<string>();
            SubLayerNameList = new List<string>();
            LayerGuidList = new List<string>();
            LayerNameList = new List<string>();
            NameGuidList = new List<SubLayerGuid>();
            SystemIdList = new List<int>();
            field_names = new Dictionary<string, string>();

            host = "data-pc";
            port = 1521;
            sid = "GEO";

            return true;
        }
        public bool tryConnect()
        {
            dbcon = GetDBConnection(host, port, sid, network_user, network_pass);
            try
            {
                dbcon.Open();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
            dbcon.Close();
            return true;
        }
        public bool tryConnectAndSave()
        {
            dbcon = GetDBConnection(host, port, sid, network_user, network_pass);
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
        public bool closeConnectAndSave()
        {
            try
            {
                dbcon.Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
            return true;
        }
        public static OracleConnection GetDBConnection(string host, int port, String sid, String user, String password)
        {
            // 'Connection String' подключается напрямую к Oracle.
            string connString = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)(HOST = "
                    + host + ")(PORT = " + port + "))(CONNECT_DATA = (SERVER = DEDICATED)(SERVICE_NAME = "
                    + sid + ")));Password=" + password + ";User ID=" + user + ";Connection Timeout = 360;";

            return new OracleConnection { ConnectionString = connString };
        }
        public bool askPassword()
        {
            SimpleLoginWindow window = new SimpleLoginWindow();
            if (window.ShowDialog() == false) return false;
            network_user = window.LoginDto.Username;
            network_pass = window.LoginDto.Password;
            return true;
        }
        public bool parseExternalDBLINK(string BaseName, out string table_data_fin, OracleConnection dbcon)
        {
            table_data_fin = "Empty";
            field_names.Clear();
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

                            if (!field_names.ContainsKey(row_0[0]))
                            {
                                field_names.Add(row_0[0], field_value);
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
        public bool getExternalDB(string BaseName, string BaseCaption, string ChildField, int SystemID, OracleConnection dbcon)
        {
            try
            {
                if (true)
                {
                    string command_str = "SELECT ";
                    int field_count = 0;
                    foreach (var item in field_names)
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
    }
}