using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;

namespace Plugins
{
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
                    Vars = Tuple.Create<DataSource, object>(DataSource.OracleDatabase, Tuple.Create<string, string, string, string>(model.UserName, SecureStringToString(passwordBox.SecurePassword), model.Host, vars[model.Privilege]));
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
        private String SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
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
