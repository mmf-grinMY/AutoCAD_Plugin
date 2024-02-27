using System.Collections.ObjectModel;
using System.Windows;
using System;

namespace Plugins.View
{
    /// <summary>
    /// Логика взаимодействия для GorizontSelecterWindow.xaml
    /// </summary>
    public partial class GorizontSelecterWindow : Window, IDisposable
    {
        /// <summary>
        /// Выбранный горизонт
        /// </summary>
        public string Gorizont { get; private set; }
        /// <summary>
        /// Результат ввода
        /// </summary>
        public bool InputResult { get; private set; }
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="dbGorizonts">Список доступных горизонтов</param>
        public GorizontSelecterWindow(ObservableCollection<string> dbGorizonts)
        {
            InitializeComponent();
            var model = new GorizontSelecterViewModel(dbGorizonts)
            {
                CancelCommand = new RelayCommand(obj =>
                {
                    InputResult = false;
                    Hide();
                })
            };
            DataContext = model;
            model.SelectCommand = new RelayCommand(obj =>
            {
                InputResult = true;
                Gorizont = model.Gorizonts[model.SelectedGorizont];
                Hide();
            });
        }
        /// <summary>
        /// Освобождение занятых ресурсов
        /// </summary>
        public void Dispose()
        {
            Close();
        }
        /// <summary>
        /// Модель представления для GorizontSelecterWindow.xaml
        /// </summary>
        private class GorizontSelecterViewModel : BaseViewModel
        {
            /// <summary>
            /// Выбранный горизонт
            /// </summary>
            private int selectedGorizont;
            /// <summary>
            /// Список доступных для чтения горизонтов
            /// </summary>
            private readonly ObservableCollection<string> gorizonts;
            /// <summary>
            /// Создание объекта
            /// </summary>
            /// <param name="dbGorizonts">Список доступных для чтения горизонтов</param>
            public GorizontSelecterViewModel(ObservableCollection<string> dbGorizonts)
            {
                gorizonts = dbGorizonts;
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
}
