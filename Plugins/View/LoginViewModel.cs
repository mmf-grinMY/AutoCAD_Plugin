using System.Collections.ObjectModel;

namespace Plugins
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
        /// Отслеживание граничных точек
        /// </summary>
        private bool checkingBoundingBox;
        /// <summary>
        /// Выбранный горизонт
        /// </summary>
        private int selectedGorizont;
        #endregion

        #region Ctors
        /// <summary>
        /// Создание объекта
        /// </summary>
        public LoginViewModel()
        {
            privilege = 0;
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
        // TODO: Считывание существующих горизонтов из таблицы
        // Алгоритм
        // Подключение к Базе данных
        // После подключения к базе идет запрос на существование набора таблиц <GOR>_trans_clone, <GOR>_trans_open_sublayers 
        // Появление окна выбора горизонта
        /// <summary>
        /// Список доступных для отрисовки горизонтов
        /// </summary>
        public ObservableCollection<string> Gorizonts { get; set; } = new ObservableCollection<string>()
        {
            "K200F",
            "K290F",
            "K290N",
            "K300E",
            "K305F",
            "K380",
            "K420F",
            "K430F",
            "K440F",
            "K445F",
            "K450",
            "K450E",
            "K450F",
            "K620F",
            "K630F",
            "K640",
            "K670F"
        };
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
