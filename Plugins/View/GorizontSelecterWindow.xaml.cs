using System.Collections.ObjectModel;
using System.Windows;

namespace Plugins.View
{
    /// <summary>
    /// Логика взаимодействия для GorizontSelecterWindow.xaml
    /// </summary>
    partial class GorizontSelecterWindow : Window, IResult
    {
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
        public string Result { get; internal set; }
        public bool IsSuccess { get; internal set; }
    }
    /// <summary>
    /// Модель представления для GorizontSelecterWindow.xaml
    /// </summary>
    class GorizontSelecterViewModel : BaseViewModel
    {
        /// <summary>
        /// Выбранный горизонт
        /// </summary>
        int selectedGorizont;
        /// <summary>
        /// Список доступных для чтения горизонтов
        /// </summary>
        readonly ObservableCollection<string> gorizonts;
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
        /// <summary>
        /// Список доступных для отрисовки горизонтов
        /// </summary>
        public ObservableCollection<string> Gorizonts => gorizonts;
        /// <summary>
        /// Команда продолжения
        /// </summary>
        public RelayCommand SelectCommand { get; set; }
        /// <summary>
        /// Команда прекращения действий
        /// </summary>
        public RelayCommand CancelCommand { get; set; }
    }
}
