#define OLD

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
#if OLD
        public (string, string, bool) ConnectionString { get; private set; }
#else
        public string ConnectionString { get; private set; }
#endif
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
                string[] vars = { "NORMAL", "SYSDBA", "SYSOPER" };
                string message = string.Empty;
                if (string.IsNullOrWhiteSpace(model.UserName))
                    message += "имя пользователя";
                string password = passwordBox.Password;
                if (string.IsNullOrWhiteSpace(password))
                    message += message == string.Empty ? "пароль" : ", пароль";
                if (string.IsNullOrWhiteSpace(model.Host))
                    message += message == string.Empty ? "имя базы данных" : ", имя базы данных";
#if OLD
                if (model.SelectedGorizont == -1)
                    message += message == string.Empty ? "Не выбран горизонт!" : "! Не выбран горизонт!";
#endif
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
#if OLD
                    ConnectionString = ($"Data Source={model.Host};Password={password};User Id={model.UserName};Connection Timeout = 360;" + 
                        (vars[model.Privilege] == "NORMAL" ? string.Empty : $"DBA Privilege={vars[model.Privilege]};"), model.Gorizonts[model.SelectedGorizont], model.CheckingBoundingBox);
#else
                    ConnectionString = $"Data Source={model.Host};Password={password};User Id={model.UserName};Connection Timeout = 360;" +
                        (vars[model.Privilege] == "NORMAL" ? string.Empty : $"DBA Privilege={vars[model.Privilege]};");
#endif
                }
            });
            model.CancelCommand = new RelayCommand(obj => 
            {
                InputResult = false;
                Close();
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
    }
}
