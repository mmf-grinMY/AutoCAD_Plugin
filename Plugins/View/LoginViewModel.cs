using System;

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
        public RelayCommand SaveCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }
    }
}
