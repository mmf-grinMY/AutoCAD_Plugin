using System.Windows;
using System.Data;
using System;

namespace Plugins.View
{
    /// <summary>
    /// Логика взаимодействия для ExternalDBWindow.xaml
    /// </summary>
    public partial class ExternalDBWindow : Window, IDisposable
    {
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="dataTable">Таблица данных</param>
        public ExternalDBWindow(DataTable dataTable)
        {
            InitializeComponent();
            dataGrid.ItemsSource = dataTable.DefaultView;
        }
        /// <summary>
        /// Освобождение занятых ресурсов
        /// </summary>
        public void Dispose()
        {
            Close();
        }
    }
}
