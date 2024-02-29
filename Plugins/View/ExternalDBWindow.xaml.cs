using System.Windows;
using System.Data;
using System;

namespace Plugins.View
{
    /// <summary>
    /// Логика взаимодействия для ExternalDbWindow.xaml
    /// </summary>
    public partial class ExternalDbWindow : Window, IDisposable
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
        /// <summary>
        /// Освобождение занятых ресурсов
        /// </summary>
        public void Dispose()
        {
            Close();
        }
        /// <summary>
        /// Внутренняя логика взаимодействия для ExternalDbWindow.xaml
        /// </summary>
        private sealed class ExternalDbViewModel : BaseViewModel
        {
            /// <summary>
            /// Данные для отображения
            /// </summary>
            public DataView View { get; }
            /// <summary>
            /// Создание объекта
            /// </summary>
            /// <param name="view">Данные для отображения</param>
            public ExternalDbViewModel(DataView view)
            {
                View = view;
            }
        }
    }
}
