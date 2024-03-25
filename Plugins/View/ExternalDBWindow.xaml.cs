using System.Windows;
using System.Data;

namespace Plugins.View
{
    /// <summary>
    /// Логика взаимодействия для ExternalDbWindow.xaml
    /// </summary>
    partial class ExternalDbWindow : Window
    {
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="view">Данные для отображения</param>
        public ExternalDbWindow(DataView view)
        {
            InitializeComponent();
            DataContext = new ExternalDbViewModel(view);
        }
    }
    /// <summary>
    /// Внутренняя логика взаимодействия для ExternalDbWindow.xaml
    /// </summary>
    class ExternalDbViewModel : BaseViewModel
    {
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="view">Данные для отображения</param>
        public ExternalDbViewModel(DataView view) => View = view;
        /// <summary>
        /// Данные для отображения
        /// </summary>
        public DataView View { get; }
    }
}
