using Plugins.Entities;
using Plugins.View;

using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Text;
using System;

namespace Plugins
{
    /// <summary>
    /// Класс-помощник для получения данных из БД
    /// </summary>
    static class DbHelper
    {
        /// <summary>
        /// Получение данных от пользователя
        /// </summary>
        /// <typeparam name="TWindow">Окно ввода</typeparam>
        /// <param name="window">Окно работы с пользователем</param>
        /// <returns>Введенные пользователем данные</returns>
        static object[] GetResult<TWindow>(TWindow window) where TWindow : Window, IResult
        {
            object result = null;
            bool isCanceled = true;

            window.ShowDialog();

            if (window.IsSuccess)
            {
                result = window.Result;
                isCanceled = false;
            }

            window.Close();

            return new object[] { result, isCanceled };
        }
        /// <summary>
        /// Получение строки подключения к БД
        /// </summary>
        public static object[] ConnectionStr => GetResult(new LoginWindow());
        /// <summary>
        /// Получение строки подключения без учета BoundingBox
        /// </summary>
        public static string SimpleConnectionStr => (GetResult(new LoginWindow(false))[0] as object[])[0].ToString();
        /// <summary>
        /// Получение рисуемого горизонта
        /// </summary>
        /// <param name="gorizonts">Список доступных для отрисовки горизонтов</param>
        /// <returns>Выбранный горизонт</returns>
        public static object[] SelectGorizont(ObservableCollection<string> gorizonts) => 
            GetResult(new GorizontSelecterWindow(gorizonts));
        /// <summary>
        /// Получение полилиний из wkt
        /// </summary>
        /// <param name="dispatcher">Диспетчер для работы с БД</param>
        /// <param name="primitive">Примитив рисуемого объекта</param>
        /// <returns>Массив полилиний</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static Autodesk.AutoCAD.DatabaseServices.Polyline[] Parse(IDbDispatcher dispatcher, Primitive primitive)
        {
            var lines = Entities.Wkt.Parser.ParsePolyline(primitive.Geometry);

            if (!lines.Any())
            {
                var geometry = dispatcher.GetLongGeometry(primitive);
                lines = Entities.Wkt.Parser.ParsePolyline(geometry);

                if (!lines.Any())
                {
                    throw new InvalidOperationException($"Не удалось получить геометрию объекта {primitive.Guid.ToString().ToUpper()}!");
                }
            }

            return lines;
        }
        /// <summary>
        /// Создание команды выборки данных
        /// </summary>
        /// <param name="baseName">Имя линкованной таблицы</param>
        /// <param name="linkField">Столбец линковки</param>
        /// <param name="systemId">Уникальный номер примитива</param>
        /// <param name="fieldNames">Список столбцов таблицы</param>
        /// <returns>Команда для получения данных</returns>
        public static string CreateCommand(string baseName, string linkField, int systemId, IDictionary<string, string> fieldNames)
        {
            var builder = new StringBuilder().Append("SELECT ");

            foreach (var item in fieldNames)
            {
                builder.Append(item.Key).Append(" as \"").Append(item.Value).Append("\"").Append(",");
            }

            builder
                .Remove(builder.Length - 1, 1)
                .Append(" FROM ")
                .Append(baseName)
                .Append(" WHERE ")
                .Append(linkField)
                .Append(" = ")
                .Append(systemId);

            return builder.ToString();
        }
        /// <summary>
        /// Получение списка столбцов таблицы
        /// </summary>
        /// <param name="fields">Исходные столбцы</param>
        /// <returns>Список столбцов</returns>
        public static Dictionary<string, string> ParseFieldNames(IEnumerable<string> fields)
        {
            bool fieldsFlag = true;
            var result = new Dictionary<string, string>();

            foreach (var field in fields)
            {
                if (fieldsFlag)
                {
                    if (field == "FIELDS")
                    {
                        fieldsFlag = false;
                    }
                    continue;
                }
                else if (field == "ENDFIELDS")
                {
                    break;
                }
                else if (field.Contains("+"))
                {
                    continue;
                }
                var rows = field.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (rows.Length <= 1) continue;

                var builder = new StringBuilder();

                for (int j = 1; j < rows.Length; ++j)
                {
                    builder.Append(rows[j]).Append("_");
                }

                if (!result.ContainsKey(rows[0]))
                {
                    result.Add(rows[0], builder.ToString());
                }
            }

            return result;
        }
    }
}