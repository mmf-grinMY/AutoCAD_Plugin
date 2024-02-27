namespace Plugins
{
    // FIXME: ??? Нужен ли отдельный класс для хранения | Стоит ли заменить на Tuple? ???
    /// <summary>
    /// Хранение линкованых столбцов
    /// </summary>
    public class LinkedDBFields
    {
        /// <summary>
        /// Имя линкованой таблицы
        /// </summary>
        public string BaseName { get; }
        /// <summary>
        /// Столбец линковки
        /// </summary>
        public string LinkedField { get; }
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="bseName">Имя линкованой таблицы</param>
        /// <param name="linkedField">Столбец линковки</param>
        public LinkedDBFields(string bseName, string linkedField)
        {
            BaseName = bseName;
            LinkedField = linkedField;
        }
    }
}