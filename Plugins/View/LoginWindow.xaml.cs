using System.Windows;
using System.IO;

using Newtonsoft.Json;

namespace Plugins.View
{
    /// <summary>
    /// Окно ввода параметров для подключения к БД
    /// </summary>
    partial class LoginWindow : Window, IResult
    {
        #region Ctor

        /// <summary>
        /// Создание объекта
        /// </summary>
        public LoginWindow()
        {
            InitializeComponent();
            SizeChanged += HandleSizeChanged;
            Top = (SystemParameters.FullPrimaryScreenHeight - ActualHeight) / 2;
            IsSuccess = false;

            var model = new LoginViewModel();
            DataContext = model;
            model.SaveCommand = new RelayCommand(obj =>
            {
                IsSuccess = true;
                Hide();
                var args = new ConnectionParams(model.UserName, passwordBox.Password, model.Host, model.Port, model.DbName);
                File.WriteAllText(Constants.DbConfigPath, JsonConvert.SerializeObject(args));
                Result = args.ToString();
            });
            model.CancelCommand = new RelayCommand(obj => Hide());
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

        public bool IsSuccess { get; internal set; }
        public string Result { get; internal set; }
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
            string content = File.ReadAllText(Constants.DbConfigPath);
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
    interface IResult
    {
        string Result { get; }
        bool IsSuccess { get; }
    }
}
