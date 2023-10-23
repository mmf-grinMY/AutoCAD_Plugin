namespace Plugins
{
    //// Подключение к источнику данных (Xml-файлы | БД)
    //// Поочердное чтение всех транзакций
    //// На основании данных столбца GEOWKT создание геометрии (пока что на одном слое)
    //// Закрытие соединения с источником данных

    // Хранение параметров отрисовки
    public class DrawParameters
    {
        public DrawSettings DrawSettings { get; set; }
        public string WKT { get; set; }
        public override string ToString()
        {
            return $"{DrawSettings};{WKT}";
        }
    }
}