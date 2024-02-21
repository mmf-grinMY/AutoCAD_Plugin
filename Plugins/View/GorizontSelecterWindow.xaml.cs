using Oracle.ManagedDataAccess.Client;
using System.Windows;

namespace Plugins.View
{
    /// <summary>
    /// Логика взаимодействия для GorizontSelecter.xaml
    /// </summary>
    public partial class GorizontSelecterWindow : Window
    {
        public GorizontSelecterWindow(OracleConnection connection)
        {
            InitializeComponent();
            DataContext = new GorizontSelecterViewModel(connection);
        }
    }
}
