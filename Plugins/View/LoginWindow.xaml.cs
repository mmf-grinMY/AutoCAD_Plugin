using System;
using System.Windows;

namespace Plugins
{
    /// <summary>
    /// Логика взаимодействия для UserControl1.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public Tuple<DataSource, object> Vars { get; set; }
        public LoginWindow()
        {
            InitializeComponent();
            this.Loaded += LoginWindow_Loaded;
            this.SizeChanged += LoginWindow_SizeChanged;
            LoginViewModel model = new LoginViewModel();
            DataContext = model;
            model.SaveCommand = new RelayCommand(obj =>
            {
                string[] vars = { "NORMAL", "SYSDBA", "SYSOPER" };
                DialogResult = true;
                if (flip.IsMainPanelOpened)
                {
                    Vars = Tuple.Create<DataSource, object>(DataSource.OracleDatabase, Tuple.Create<string, string, string, string>(model.UserName, model.Password, model.Host, vars[model.Privilege]));
                }
                else
                {
                    Vars = Tuple.Create<DataSource, object>(DataSource.XmlDocument, Tuple.Create<string, string>(model.Geometry, model.Layers));
                }
                Close();
            });
            model.CancelCommand = new RelayCommand(obj => 
            {
                DialogResult = false;
                Close();
            });
        }
        private readonly double _screenWidth = SystemParameters.FullPrimaryScreenWidth;
        private readonly double _screenHeight = SystemParameters.FullPrimaryScreenHeight;
        private void LoginWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Top = (_screenHeight - ActualHeight) / 2;
            this.Left = (_screenWidth - ActualWidth) / 2;
        }
        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            flip.OtherInput.Visibility = Visibility.Collapsed;
        }
    }
}
