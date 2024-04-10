using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Plugins.View
{
    /// <summary>
    /// Логика взаимодействия для GorizontSelecterWindow.xaml
    /// </summary>
    partial class GorizontSelecterWindow : Window, IResult
    {
        #region Ctor

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="dbGorizonts">Список доступных горизонтов</param>
        public GorizontSelecterWindow(ObservableCollection<string> dbGorizonts)
        {
            InitializeComponent();
            IsSuccess = false;

            var model = new GorizontSelecterViewModel(dbGorizonts);
            DataContext = model;
            model.CancelCommand = new RelayCommand(obj => Hide());
            model.SelectCommand = new RelayCommand(obj =>
            {
                IsSuccess = true;
                Result = model.Gorizonts[model.SelectedGorizont];
                Hide();
            });
        }

        #endregion

        #region Public Properties

        public object Result { get; internal set; }
        public bool IsSuccess { get; internal set; }

        #endregion
    }
    /// <summary>
    /// Модель представления для GorizontSelecterWindow.xaml
    /// </summary>
    class GorizontSelecterViewModel : BaseViewModel
    {
        #region Private Fields

        /// <summary>
        /// Выбранный горизонт
        /// </summary>
        int selectedGorizont;
        /// <summary>
        /// Список доступных для чтения горизонтов
        /// </summary>
        readonly ObservableCollection<string> gorizonts;

        #endregion

        #region Ctor

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="dbGorizonts">Список доступных для чтения горизонтов</param>
        public GorizontSelecterViewModel(ObservableCollection<string> dbGorizonts) => gorizonts = dbGorizonts;
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

        #endregion

        #region Public Properties

        /// <inheritdoc cref="gorizonts"/>
        public ObservableCollection<string> Gorizonts => gorizonts;

        #endregion

        #region Commands

        /// <summary>
        /// Команда продолжения
        /// </summary>
        public ICommand SelectCommand { get; set; }
        /// <summary>
        /// Команда прекращения действий
        /// </summary>
        public ICommand CancelCommand { get; set; }

        #endregion
    }
}
