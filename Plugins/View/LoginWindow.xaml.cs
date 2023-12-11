using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;

namespace Plugins
{
    public partial class LoginWindow : Window
    {
        internal WindowVars Vars { get; set; }
        public LoginWindow()
        {
            InitializeComponent();
            Loaded += LoginWindow_Loaded;
            SizeChanged += LoginWindow_SizeChanged;
            LoginViewModel model = new LoginViewModel();
            DataContext = model;
            model.SaveCommand = new RelayCommand(obj =>
            {
                string[] vars = { "NORMAL", "SYSDBA", "SYSOPER" };
                DialogResult = true;
                if (flip.IsMainPanelOpened)
                {
                    Vars = new DBWindowVars(model.UserName,
                                            SecureStringToString(passwordBox.SecurePassword),
                                            model.Host,
                                            vars[model.Privilege],
                                            model.TransactionTableName,
                                            model.LayersTableName);
                }
                else
                {
                    Vars = new XmlWindowVars(model.Geometry,
                                             model.Layers);
                }
                Close();
            });
            model.CancelCommand = new RelayCommand(obj => 
            {
                DialogResult = false;
                Close();
            });
        }
        private string SecureStringToString(SecureString value)
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
            Top = (_screenHeight - ActualHeight) / 2;
            Left = (_screenWidth - ActualWidth) / 2;
        }
        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            flip.OtherInput.Visibility = Visibility.Collapsed;
        }
    }
}
