namespace Plugins.View
{
    internal class SimpleLoginViewModel : BaseViewModel
    {
        #region Private Fields
        /// <summary>
        /// Имя пользователя
        /// </summary>
        private string username;
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
    public class SimpleLoginDto
    {
        public SimpleLoginDto(string username, string password)
        {
            Username = username;
            Password = password;
        }
        public string Username { get; }
        public string Password { get; }
    }
}
