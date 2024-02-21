#define OLD

using System.Collections.ObjectModel;
using System.IO;

namespace Plugins.View
{
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
        /// <summary>
        /// Привелегии пользователя
        /// </summary>
        private int privilege;
        /// <summary>
        /// Выбранный горизонт
        /// </summary>
        private int selectedGorizont;
#if OLD
        /// <summary>
        /// Отслеживание граничных точек
        /// </summary>
        private bool checkingBoundingBox;
#endif
        #endregion

        #region Ctors
        /// <summary>
        /// Создание объекта
        /// </summary>
        public LoginViewModel()
        {
            privilege = 0;
            Gorizonts = new ObservableCollection<string>();
            using(var reader = new StreamReader(Path.Combine(Constants.SupportPath, "gorizonts.txt")))
            {
                var content = reader.ReadToEnd();
                foreach(var item in content.Split(',')) 
                {
                    Gorizonts.Add(item);
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
        /// <summary>
        /// Привелегии пользователя
        /// </summary>
        public int Privilege
        {
            get => privilege;
            set
            {
                privilege = value;
                OnPropertyChanged(nameof(Privilege));
            }
        }
        /// <summary>
        /// Выбранный горизонт
        /// </summary>
        public int SelectedGorizont
        {
            get => selectedGorizont;
            set
            {
                selectedGorizont = value;
                OnPropertyChanged(nameof(SelectedGorizont));
            }
        }
        public ObservableCollection<string> Gorizonts { get; }
#if OLD
        /// <summary>
        /// Отслеживание граничных точек
        /// </summary>
        public bool CheckingBoundingBox
        {
            get => checkingBoundingBox;
            set
            {
                checkingBoundingBox = value;
                OnPropertyChanged(nameof(CheckingBoundingBox));
            }
        }
#endif 
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
