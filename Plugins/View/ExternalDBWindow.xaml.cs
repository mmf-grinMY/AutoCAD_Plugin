using System;
using System.Data;
using System.Windows;

namespace Plugins.View
{
    /// <summary>
    /// Логика взаимодействия для ExternalDBWindow.xaml
    /// </summary>
    public partial class ExternalDBWindow : Window, IDisposable
    {
        public ExternalDBWindow(DataTable dataTable)
        {
            InitializeComponent();
            dataGrid.ItemsSource = dataTable.DefaultView;
        }
        public void Dispose()
        {
            if (IsLoaded)
                Close();
        }
    }
}
