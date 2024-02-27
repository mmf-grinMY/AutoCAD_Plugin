namespace Plugins
{
    /// <summary>
    /// Пользовательское исключение для экстренного перехода в вызывающую функцию
    /// </summary>
    internal class GotoException : System.Exception
    {
        /// <summary>
        /// Порядковый номер исключительной ситуации
        /// </summary>
        private readonly int index;
        /// <summary>
        /// Причина исключения
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">Возникает при выходе индекса за границы</exception>
        public string RowName
        {
            get
            {
                switch (index)
                {
                    case 0: return "drawjson";
                    case 1: return "geowkt";
                    case 2: return "paramjson";
                    case 3: return "sublayerguid";
                    case 4: return "Ошибка при создании объекта DrawParams";
                    default: throw new System.ArgumentOutOfRangeException(nameof(index), "должен быть в диапазоне от 0 до 4");
                }
            }
        }
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="index">Порядковый номер исключительной ситуации</param>
        public GotoException(int index) { this.index = index; }
    }
}