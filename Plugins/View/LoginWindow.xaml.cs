using System.IO;
using System.Text.Json;
using System.Windows;

namespace Plugins.View
{
    /// <summary>
    /// Окно ввода параметров для подключения к БД
    /// </summary>
    public partial class LoginWindow : Window
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
            SizeChanged += LoginWindow_SizeChanged;
            LoginViewModel model = new LoginViewModel();
            DataContext = model;
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
                    var strings = model.Host.Split('/');
                    string host = string.Empty;
                    string sid = string.Empty;
                    int port = 1521;
                    if (strings.Length == 1)
                    {
                        sid = strings[0];
                    }
                    else if (strings.Length == 2)
                    {
                        host = strings[0];
                        sid = strings[1];
                    }
                    Params = new ConnectionParams(model.UserName, password, host, port, sid);
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
        /// Изменить размер окна
        /// </summary>
        /// <param name="sender">Вызывающий объект</param>
        /// <param name="e">Параметры вызова</param>
        private void LoginWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Top = (SystemParameters.FullPrimaryScreenWidth - ActualHeight) / 2;
            Left = (SystemParameters.FullPrimaryScreenHeight - ActualWidth) / 2;
        }
        #endregion
        /// <summary>
        /// Модель предстваления для класса LoginWindow
        /// </summary>
        internal class LoginViewModel : BaseViewModel
        {
            #region Private Fields
            /// <summary>
            /// Имя пользователя
            /// </summary>
            private string username;
            /// <summary>
            /// Местоположение базы данных
            /// </summary>
            private string host;
            #endregion

            #region Ctors
            /// <summary>
            /// Создание объекта
            /// </summary>
            public LoginViewModel()
            {
                string content = File.ReadAllText(Constants.DbConfigFilePath);
                if (content != string.Empty)
                {
                    MessageBox.Show(content);
                    var root = JsonDocument.Parse(content).RootElement;
                    if (root.TryGetProperty("UserName", out JsonElement username))
                    {
                        UserName = username.GetString();
                        Host = root.GetProperty("Host").GetString() + "/" + root.GetProperty("Sid").GetString();
                    }
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
            #endregion

            #region Commands
            /// <summary>
            /// Поключиться
            /// </summary>
            public RelayCommand SaveCommand { get; set; }
            /// <summary>
            /// Отменить подключение
            /// </summary>
            public RelayCommand CancelCommand { get; set; }
            #endregion
        }
    }
}
