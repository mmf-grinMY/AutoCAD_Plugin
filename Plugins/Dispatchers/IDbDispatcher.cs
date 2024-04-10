using System.Collections.Generic;

namespace Plugins
{
    /// <summary>
    /// Диспетчер для работы с БД
    /// </summary>
    interface IDbDispatcher : System.IDisposable
    {
        #region Properties

        /// <summary>
        /// Количество записей на горизонте, доступных для отрисовки
        /// </summary>
        /// <param name="gorizont">Имя горизонта для поиска</param>
        /// <returns>Количество записей</returns>
        uint Count { get; }
        /// <summary>
        /// Доступные для отрисовки горизонты
        /// </summary>
        System.Collections.ObjectModel.ObservableCollection<string> Gorizonts { get; }
        /// <summary>
        /// Учет граничных точек
        /// </summary>
        bool IsBoundingBoxChecked { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Получение линковки
        /// </summary>
        /// <param name="baseName">Имя таблицы для линковки</param>
        /// <returns></returns>
        string GetExternalDbLink(string baseName);
        /// <summary>
        /// Получение таблицы данных
        /// </summary>
        /// <param name="command">Команда для получения данных</param>
        /// <returns>Таблица данных</returns>
        System.Data.DataTable GetDataTable(string command);
        /// <summary>
        /// Получение геометрии больших объектов
        /// </summary>
        /// <param name="primitive">Исходный примитив</param>
        /// <returns>Геометрия в формате wkt</returns>
        string GetLongGeometry(Entities.Primitive primitive);
        /// <summary>
        /// Логика чтения данных о примитиве из БД
        /// </summary>
        /// <param name="token">Токен остановки потока</param>
        /// <param name="queue">Очередь на запись</param>
        /// <param name="model">Логика работы процесса записи</param>
        /// <param name="session">Текущая сессия работы команды</param>
        void ReadAsync(System.Threading.CancellationToken token,
                       System.Collections.Concurrent.ConcurrentQueue<Entities.Primitive> queue,
                       View.DrawInfoViewModel model,
                       Session session);
        /// <summary>
        /// Получение списка необходимых для записи слоев
        /// </summary>
        /// <returns>Список необходимых для записи слоев</returns>
        IEnumerable<string> GetLayers();

        #endregion
    }
}