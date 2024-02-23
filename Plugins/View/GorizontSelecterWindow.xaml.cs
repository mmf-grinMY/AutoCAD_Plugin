using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace Plugins.View
{
    /// <summary>
    /// Логика взаимодействия для GorizontSelecter.xaml
    /// </summary>
    public partial class GorizontSelecterWindow : Window
    {
        public string Gorizont { get; private set; }
        public bool InputResult { get; private set; }
        public GorizontSelecterWindow(OracleConnection connection)
        {
            InitializeComponent();
            var model = new GorizontSelecterViewModel(connection);
            DataContext = model;
            model.SelectCommand = new RelayCommand(obj =>
            {
                InputResult = true;
                Gorizont = model.Gorizonts[model.SelectedGorizont];
                Hide();
            });
            model.CancelCommand = new RelayCommand(obj =>
            {
                InputResult = false;
                Hide();
            });
        }
        private class GorizontSelecterViewModel : BaseViewModel
        {
            /// <summary>
            /// Выбранный горизонт
            /// </summary>
            private int selectedGorizont;
            private readonly ObservableCollection<string> gorizonts;
            public GorizontSelecterViewModel(OracleConnection connection)
            {
                gorizonts = new ObservableCollection<string>();
                IDictionary<string, bool> selectedGorizonts = new Dictionary<string, bool>();
                using (var reader = new OracleCommand("SELECT table_name FROM all_tables WHERE table_name LIKE 'K%_TRANS_CLONE' OR table_name LIKE 'K%_TRANS_OPEN_SUBLAYERS'", connection).ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string tableName = reader.GetString(0).Split('_')[0];
                        if (selectedGorizonts.ContainsKey(tableName))
                        {
                            selectedGorizonts[tableName] = true;
                        }
                        else
                        {
                            selectedGorizonts.Add(tableName, false);
                        }
                    }
                }
                foreach (var key in selectedGorizonts.Keys)
                {
                    if (selectedGorizonts[key])
                    {
                        gorizonts.Add(key);
                    }
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
            /// <summary>
            /// Список доступных для отрисовки горизонтов
            /// </summary>
            public ObservableCollection<string> Gorizonts => gorizonts;
            public RelayCommand SelectCommand { get; set; }
            public RelayCommand CancelCommand { get; set; }
        }
    }
}
