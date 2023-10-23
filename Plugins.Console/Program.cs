using Plugins;
using System.Collections.Generic;
using System;
using System.Windows;
using static Plugins.Commands;

internal class Program
{
    [STAThread]
    public static void Main()
    {
        DataSource dataSource;
        List<DrawParameters> drawParameters;
        string user, password, host, privilege;
        try
        {
            var window = new LoginWindow();
            bool? resultDialog = window.ShowDialog();

            if (resultDialog.HasValue && window.Vars != null)
            {
                (user, password, host, privilege) = window.Vars;
                dataSource = DataSource.OracleDatabase;
            }
            else
            {
                MessageBox.Show("Для отрисовки объектов требуются данные!");
                return;
            }

            switch (dataSource)
            {
                case DataSource.OracleDatabase: drawParameters = LoadDataFromDB(user, password, host, privilege); break;
                case DataSource.XmlDocument: drawParameters = LoadDataFromXml(); break;
                default: drawParameters = new List<DrawParameters>(); break;
            }

            MessageBox.Show(drawParameters.Count.ToString());
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
}