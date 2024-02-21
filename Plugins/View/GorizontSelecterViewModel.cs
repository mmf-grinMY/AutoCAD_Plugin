using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Plugins.View
{
    internal class GorizontSelecterViewModel : BaseViewModel
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
                    string tableName = reader.GetString(0).Substring(1, 4);
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
                    gorizonts.Add(key.Replace("_", ""));
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
        // TODO: Считывание существующих горизонтов из таблицы
        // Алгоритм
        // Подключение к Базе данных
        // После подключения к базе идет запрос на существование набора таблиц <GOR>_trans_clone, <GOR>_trans_open_sublayers 
        // Появление окна выбора горизонта
        /// <summary>
        /// Список доступных для отрисовки горизонтов
        /// </summary>
#if OLD
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
#else
        public ObservableCollection<string> Gorizonts => gorizonts;
#endif
    }
}
