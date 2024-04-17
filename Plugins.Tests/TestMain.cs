using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using System;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

using Oracle.ManagedDataAccess.Client;

[assembly: CommandClass(typeof(Plugins.Tests.TestMain))]

namespace Plugins.Tests
{
    class TestMain
    {
        static TestMain()
        {
            var parts = Regex.Split(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(typeof(Commands)).Location), "test.ini")),"\\r\\n");
            CONNECTION_STRING = parts[0];
            GORIZONT = parts[1];
        }
        /// <summary>
        /// Получение корректного названия слоя AutoCAD
        /// </summary>
        /// <param name="layername">Имя слоя MapManager</param>
        /// <param name="sublayername">Имя подслоя MapManager</param>
        /// <returns>Корректное имя слоя</returns>
        static string GetLayerName(string layername, string sublayername) =>
            Regex.Replace(layername + " _ " + sublayername, "[<>\\*\\?/|\\\\\":;,=]", "_");
        /// <summary>
        /// Тестирвемый горизонт
        /// </summary>
        static readonly string GORIZONT;
        /// <summary>
        /// Строка подключения
        /// </summary>
        static readonly string CONNECTION_STRING;
        /// <summary>
        /// Тестирование корректности отрисовки командой MMP_DRAW
        /// </summary>
        [CommandMethod("MMP_TEST")]
        public void Test()
        {
            var layers = Test_OA_Layers();
            if (layers.Count() > 0)
            {
                System.Windows.MessageBox.Show(layers.Aggregate((i, j) => i + '\n' + j));
                return;
            }

            var nullLayer = "0";
            var count = GetEntitiesOnLayer(Filter.GetAll(nullLayer)).Count;
            if (count != 0)
            {
                System.Windows.MessageBox.Show($"Слой \"{nullLayer}\" содержит объекты в количестве {count}!");
                return;
            }

            var dict = GetEntitiesSub(Filter.GetText, "LabelDraw");
            if (dict.Count > 0)
            {
                System.Windows.MessageBox.Show(string.Join("\n", dict.Select(p => $"{p.Key}: {p.Value}")));
                return;
            }

            dict = GetEntitiesSub(Filter.GetSign, "Sign");
            if (dict.Count > 0)
            {
                System.Windows.MessageBox.Show(string.Join("\n", dict.Select(p => $"{p.Key}: {p.Value}")));
                return;
            }

            System.Windows.MessageBox.Show("Все тесты успешно выполнены!");
        }
        /// <summary>
        /// Тестирование корректности количества слоев
        /// </summary>
        /// <returns>Некорректные слои</returns>
        IEnumerable<string> Test_OA_Layers()
        {
            var db = Application.DocumentManager.MdiActiveDocument.Database;

            OracleConnection connection = null;
            TSTransaction transaction = null;
            IEnumerable<string> wrongLayers = new List<string>();

            try
            {
                connection = new OracleConnection(CONNECTION_STRING);
                connection.Open();

                transaction = new TSTransaction();

                foreach (var layerId in transaction.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable)
                {
                    var name = (transaction.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord).Name;
                    var layerParts = Regex.Split(name, " _ ");
                    try
                    {
                        var command = $"SELECT COUNT(*) FROM k{GORIZONT}_trans_open_sublayers WHERE layername = '{layerParts[0]}' " +
                            // FIXME: подслой может содержать другой неподдерживаемый символ
                            $"AND sublayername = '{layerParts[1].Replace('_', ':')}'";

                        using (var reader = new OracleCommand(command, connection).ExecuteReader())
                        {
                            if (!(reader.Read() && reader.GetInt32(0) == 1))
                                wrongLayers.Append(name);
                        }
                    }
                    catch (IndexOutOfRangeException)
                    {
                        continue;
                    }
                }

                return wrongLayers;
            }
            finally
            {
                transaction?.Dispose();
                connection?.Dispose();
            }
        }
        /// <summary>
        /// Получение объектов, находящихся на слое
        /// </summary>
        /// <param name="values">Фильтр выбора</param>
        /// <returns>Коллекция Id выбранных объектов</returns>
        ObjectIdCollection GetEntitiesOnLayer(TypedValue[] values)
        {
            var editor = Application.DocumentManager.MdiActiveDocument.Editor;
            var filter = new SelectionFilter(values);
            var selectionResult = editor.SelectAll(filter);

            return selectionResult.Status == PromptStatus.OK
                ? new ObjectIdCollection(selectionResult.Value.GetObjectIds())
                : new ObjectIdCollection();
        }
        /// <summary>
        /// Получение пар [слой, количества незаписанных объектов]
        /// </summary>
        /// <param name="filter">Метод фильтрации</param>
        /// <param name="pattern">Паттерн типа объекта</param>
        /// <returns>Словарь ошибочно записанных слоев</returns>
        IDictionary<string, int> GetEntitiesSub(Func<string, TypedValue[]> filter, string pattern)
        {
            var result = new Dictionary<string, int>();

            using (var connection = new OracleConnection(CONNECTION_STRING))
            {
                connection.Open();

                var command = string.Format("SELECT DISTINCT b.layername, b.sublayername, COUNT(*) FROM k{0}_trans_clone a " +
                    "INNER JOIN k{0}_trans_open_sublayers b ON a.sublayerguid = b.sublayerguid " +
                    "WHERE a.geowkt IS NOT NULL AND a.drawjson LIKE '%{1}%' GROUP BY b.layername, b.sublayername", GORIZONT, pattern);
                using (var reader = new OracleCommand(command, connection).ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var layerName = GetLayerName(reader.GetString(0), reader.GetString(1));
                        var expected = reader.GetInt32(2);
                        var actual = GetEntitiesOnLayer(filter(layerName)).Count;
                        if (expected != actual) result.Add(layerName, actual - expected);
                    }
                }
            }

            return result;
        }
        static class Filter
        {
            public static TypedValue[] GetAll(string layerName) => 
                new TypedValue[] { new TypedValue((int)DxfCode.LayerName, layerName) };
            public static TypedValue[] GetText(string layerName) =>
                new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Operator, "<and"),
                    new TypedValue((int)DxfCode.LayerName, layerName),
                    new TypedValue((int)DxfCode.Start, "TEXT"),
                    new TypedValue((int)DxfCode.Operator, "and>"),
                };
            public static TypedValue[] GetSign(string layerName) =>
                new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Operator, "<and"),
                    new TypedValue((int)DxfCode.LayerName, layerName),
                    new TypedValue((int)DxfCode.Operator, "<and"),
                    new TypedValue((int)DxfCode.Start, "INSERT"),
                    new TypedValue((int)DxfCode.BlockName, "*chr`#*"),
                    new TypedValue((int)DxfCode.Operator, "and>"),
                    new TypedValue((int)DxfCode.Operator, "and>"),
                };
        }
    }
    /// <summary>
    /// Throw Safe Transaction
    /// </summary>
    class TSTransaction : IDisposable
    {
        readonly Transaction transaction;
        internal TSTransaction() =>
            transaction = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction();
        public void Dispose()
        {
            transaction.Commit();
            transaction.Dispose();
        }
        public DBObject GetObject(ObjectId id, OpenMode openMode) => transaction.GetObject(id, openMode);
    }
}
