using System.Windows;
using System.IO;

using Newtonsoft.Json;
using System.Collections.Generic;

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
        public LoginWindow(bool isBBChecked = true)
        {
            InitializeComponent();

            if (!isBBChecked)
                BBCheckBox.Visibility = Visibility.Collapsed;

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
                Result = new object[] { args.ToString(), model.IsBoundigBoxChecked };
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

        #region Public Properties

        public bool IsSuccess { get; internal set; }
        public object Result { get; internal set; }

        #endregion
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
        /// <summary>
        /// Учет граничных точек
        /// </summary>
        bool isBoundigBoxChecked;

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

        /// <inheritdoc cref="username"/>
        public string UserName
        {
            get => username;
            set
            {
                username = value;
                OnPropertyChanged(nameof(UserName));
            }
        }
        /// <inheritdoc cref="host"/>
        public string Host
        {
            get => host;
            set
            {
                host = value;
                OnPropertyChanged(nameof(Host));
            }
        }
        /// <inheritdoc cref="dbName"/>
        public string DbName
        {
            get => dbName;
            set
            {
                dbName = value;
                OnPropertyChanged(nameof(DbName));
            }
        }        
        /// <inheritdoc cref="port"/>
        public int Port
        {
            get => port;
            set
            {
                port = value;
                OnPropertyChanged(nameof(Port));
            }
        }
        /// <inheritdoc cref="isBoundigBoxChecked"/>
        public bool IsBoundigBoxChecked
        {
            get => isBoundigBoxChecked;
            set
            {
                isBoundigBoxChecked = value;
                OnPropertyChanged(nameof(IsBoundigBoxChecked));
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
        /// <summary>
        /// Введенные пользователем данные
        /// </summary>
        object Result { get; }
        /// <summary>
        /// Подтверждение операции ввода
        /// </summary>
        bool IsSuccess { get; }
    }
}
