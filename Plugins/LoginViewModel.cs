using System;
using System.Windows;

namespace Plugins
{
    internal class LoginViewModel : BaseViewModel
    {
        private string _username;
        public string UserName 
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged(nameof(UserName));
            }
        }
        private string _password;
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }
        private string _host;
        public string Host
        {
            get => _host;
            set
            {
                _host = value;
                OnPropertyChanged(nameof(Host));
            }
        }
        private int _privilege;
        public int Privilege
        {
            get => _privilege;
            set
            {
                _privilege = value;
                OnPropertyChanged(nameof(Privilege));
            }
        }
        private string _geometry;
        public string Geometry
        {
            get => _geometry;
            set
            {
                _geometry = value;
                OnPropertyChanged(nameof(Geometry));
            }
        }
        private string _layers;
        public string Layers
        {
            get => _layers;
            set
            {
                _layers = value;
                OnPropertyChanged(nameof(Layers));
            }
        }
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }
        private readonly LoginWindow _window;
        private readonly Tuple<string, string, string, string> _tuple;
        public LoginViewModel(LoginWindow window)
        {
            _window = window;
            SaveCommand = new RelayCommand(obj =>
            {
                string[] vars = { "NORMAL", "SYSDBA", "SYSOPER" };
                _window.DialogResult = true;
                _window.Vars = Tuple.Create<string, string, string, string>(UserName, Password, Host, vars[Privilege]);
                _window.Close();

            });
            CancelCommand = new RelayCommand(obj => {
                _window.DialogResult = false;
                _window.Close();
            });
        }
    }
}
