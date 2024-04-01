using System.Windows;
using System.IO;
using System;

using Newtonsoft.Json;

namespace Plugins.View
{
    /// <summary>
    /// Окно ввода параметров для подключения к БД
    /// </summary>
    partial class LoginWindow : Window, IDisposable
    {
        #region Public Properties
        /// <summary>
        /// Строка подключения
        /// </summary>
        public ConnectionParams Params { get; private set; }
        /// <summary>
        /// Результат ввода информации
        /// </summary>
        public bool InputResult { get; private set; }
        #endregion

        #region Ctors
        /// <summary>
        /// Создание объекта
        /// </summary>
        public LoginWindow()
        {
            InitializeComponent();
            SizeChanged += HandleSizeChanged;
            LoginViewModel model = new LoginViewModel();
            DataContext = model;
            Top = (SystemParameters.FullPrimaryScreenHeight - ActualHeight) / 2;
            model.SaveCommand = new RelayCommand(obj =>
            {
                string message = string.Empty;
                if (string.IsNullOrWhiteSpace(model.UserName))
                    message += "имя пользователя";
                string password = passwordBox.Password;
                if (string.IsNullOrWhiteSpace(password))
                    message += message == string.Empty ? "пароль" : ", пароль";
                if (string.IsNullOrWhiteSpace(model.Host))
                    message += message == string.Empty ? "имя базы данных" : ", имя базы данных";
                if (message != string.Empty)
                {
                    message = "Некорректно введено " + message;
                    Hide();
                    MessageBox.Show(message);
                    ShowDialog();
                }
                else
                {
                    InputResult = true;
                    Hide();
                    File.WriteAllText(Constants.dbConfigPath, JsonConvert.SerializeObject(
                        Params = new ConnectionParams(model.UserName, password, model.Host, model.Port, model.DbName)));
                }
            });
            model.CancelCommand = new RelayCommand(obj => 
            {
                InputResult = false;
                Hide();
            });
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Изменение размера окна
        /// </summary>
        /// <param name="sender">Вызывающий объект</param>
        /// <param name="e">Параметры вызова</param>
        private void HandleSizeChanged(object sender, SizeChangedEventArgs e)
        {
            double left = (SystemParameters.FullPrimaryScreenWidth - ActualWidth) / 2;
            if (Left != left)
                Left = left;
        }
        #endregion

        public void Dispose()
        {
            Close();
        }
    }
    /// <summary>
    /// Модель предстваления для класса LoginWindow
    /// </summary>
    class LoginViewModel : BaseViewModel
    {
        #region Private Fields

        /// <summary>
        /// Имя пользователя
        /// </summary>
        string username;
        /// <summary>
        /// Местоположение базы данных
        /// </summary>
        string host;
        /// <summary>
        /// Имя БД
        /// </summary>
        string dbName;
        /// <summary>
        /// Номер порта
        /// </summary>
        int port;

        #endregion

        #region Ctors

        /// <summary>
        /// Создание объекта
        /// </summary>
        public LoginViewModel()
        {
            string content = File.ReadAllText(Constants.dbConfigPath);
            if (content != string.Empty)
            {
                var obj = JsonConvert.DeserializeObject<ConnectionParams>(content);
                UserName = obj.UserName;
                Host = obj.Host;
                DbName = obj.Sid;
                Port = obj.Port;
            }
            else
            {
                Port = 1521;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Имя пользователя
        /// </summary>
        public string UserName
        {
            get => username;
            set
            {
                username = value;
                OnPropertyChanged(nameof(UserName));
            }
        }
        /// <summary>
        /// Местоположение базы данных
        /// </summary>
        public string Host
        {
            get => host;
            set
            {
                host = value;
                OnPropertyChanged(nameof(Host));
            }
        }
        /// <summary>
        /// Имя базы данных
        /// </summary>
        public string DbName
        {
            get => dbName;
            set
            {
                dbName = value;
                OnPropertyChanged(nameof(DbName));
            }
        }
        /// <summary>
        /// Номер порта
        /// </summary>
        public int Port
        {
            get => port;
            set
            {
                port = value;
                OnPropertyChanged(nameof(Port));
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Поключиться к БД
        /// </summary>
        public RelayCommand SaveCommand { get; set; }
        /// <summary>
        /// Отменить подключение
        /// </summary>
        public RelayCommand CancelCommand { get; set; }

        #endregion
    }
}
