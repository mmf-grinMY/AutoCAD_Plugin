using Plugins.Tests.Internal;
using Plugins.Logging;

using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

using Oracle.ManagedDataAccess.Client;

[assembly: CommandClass(typeof(Plugins.Tests.TestMain))]

namespace Plugins.Tests
{
    class TestMain
    {
        static string GetLayerName(string layername, string sublayername) =>
            Regex.Replace(layername + " _ " + sublayername, "[<>\\*\\?/|\\\\\":;,=]", "_");
        const string GORIZONT = "305f";
        static readonly string CONNECTION_STRING = System.IO.File.ReadAllText("connection.txt");
        // Проверка записи всех слоев, содержащих объекты
        [CommandMethod("TEST_ALL_LAYERS")]
        public void Test_CheckLayers()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;

            try
            {
                var expected = new List<string> { "0" };

                using (var connection = new OracleConnection(CONNECTION_STRING))
                {
                    connection.Open();

                    string command = "SELECT DISTINCT layername, sublayername FROM (" +
                        $"SELECT b.layername, b.sublayername FROM k{GORIZONT}_trans_clone a " +
                        $"INNER JOIN k{GORIZONT}_trans_open_sublayers b ON a.sublayerguid = b.sublayerguid)";

                    using (var reader = new OracleCommand(command, connection).ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            expected.Add(reader.GetString(0) + " _ " + reader.GetString(1));
                        }
                    }
                }

                expected = expected.Select(x => Regex.Replace(x, "[<>\\*\\?/|\\\\\":;,=]", "_")).ToList();

                var db = doc.Database;
                var actual = new List<string>();

                using (var transaction = db.TransactionManager.StartTransaction())
                {
                    var table = transaction.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    foreach (var id in table)
                    {
                        actual.Add((transaction.GetObject(id, OpenMode.ForRead) as LayerTableRecord).Name);
                    }

                    transaction.Commit();
                }

                AssertCollection.AreEqual(expected, actual);

                doc.Editor.WriteMessage("Таблица слоев полна!" + Environment.NewLine);
            }
            catch (AssertException e)
            {
                doc.Editor.WriteMessage(e.Message + Environment.NewLine);
            }
        }
        [CommandMethod("TEST_ABS_COUNT")] // Не проходит
        public void Test_AbsCount()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;

            try
            {
                int expected = -1;

                using (var connection = new OracleConnection(CONNECTION_STRING))
                {
                    connection.Open();
                    string command = $"SELECT COUNT(*) FROM k{GORIZONT}_trans_clone WHERE geowkt IS NOT NULL";

                    using (var reader = new OracleCommand(command, connection).ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            expected = reader.GetInt32(0);
                        }
                    }

                    if (expected < 0) throw new AssertException("Не удалось посчитать количество объектов!" + Environment.NewLine);
                }

                var db = doc.Database;
                Transaction transaction = null;

                int actual = -1;

                try
                {
                    transaction = db.TransactionManager.StartTransaction();

                    var record = transaction.GetObject(db.CurrentSpaceId, OpenMode.ForRead) as BlockTableRecord;
                    foreach (var id in record)
                    {
                        if (transaction.GetObject(id, OpenMode.ForRead) is Entity)
                        {
                            ++actual;
                        }
                    }
                }
                finally
                {
                    if (transaction != null)
                    {
                        transaction.Commit();
                        transaction.Dispose();
                    }
                }

                Assert.AreEqual(expected, actual);

                doc.Editor.WriteMessage("Записано правильное количетство объектов!" + Environment.NewLine);
            }
            catch (AssertException e)
            {
                doc.Editor.WriteMessage(e.Message);
            }
        }
        [CommandMethod("TEST_ENTITIES_COUNT_IN_LAYERS")]
        public void Test_CountEntitiesInLayers()
        {
            var logger = new FileLogger("test_count_layer.log");
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var actual = new Dictionary<string, int>();
            var expected = new Dictionary<string, int>() { { "0", 0 } };

            using (var connection = new OracleConnection(CONNECTION_STRING))
            {
                connection.Open();
                var command = "SELECT a.layername, a.sublayername, COUNT(*) " +
                    $"FROM k{GORIZONT}_trans_open_sublayers a INNER JOIN k{GORIZONT}_trans_clone b " +
                    "ON a.sublayerguid = b.sublayerguid GROUP BY a.layername, a.sublayername";

                using (var reader = new OracleCommand(command, connection).ExecuteReader())
                {
                    while (reader.Read())
                    {
                        expected.Add(GetLayerName(reader.GetString(0), reader.GetString(1)), reader.GetInt32(2));
                    }
                }
            }

            using (var transaction = new TSTransaction())
            {
                foreach (var id in transaction.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable)
                {
                    actual.Add((transaction.GetObject(id, OpenMode.ForRead) as LayerTableRecord).Name, 0);
                }
            }

            using (var transaction = new TSTransaction())
            {
                var record = transaction.GetObject(db.CurrentSpaceId, OpenMode.ForRead) as BlockTableRecord;

                foreach (var id in record)
                {
                    if (transaction.GetObject(id, OpenMode.ForRead) is Entity entity)
                    {
                        actual[entity.Layer]++;
                    }
                }
            }

            foreach (var id in actual.Keys)
            {
                if (!expected.ContainsKey(id))
                    logger.LogInformation("Чертеж содержит лишний слой " + id + "!" + Environment.NewLine);
                else if (actual[id] != expected[id])
                    logger.LogInformation($"На слое {id} содержится {actual[id]} объектов, хотя должно быть {expected[id]}");
            }

            doc.Editor.WriteMessage("Тест закончил выполнение!" + Environment.NewLine);
        }
    }
    namespace Internal
    {
        static class AssertCollection
        {
            public static void AreEqual<T>(IEnumerable<T> actual, IEnumerable<T> expected)
            {
                if (actual == expected) return;
                if (actual.Count() != expected.Count())
                    throw new AssertException("Количество элементов в коллекции отличается:" + Environment.NewLine +
                        "Actual: " + actual.Count() + Environment.NewLine + "Expected: " + expected.Count());

                foreach (var item in actual)
                {
                    if (!expected.Contains(item)) throw new AssertException("Коллекции отличаются элементом " + item.ToString() + "!");
                }
            }
        }
        static class Assert
        {
            public static void AreEqual(object actual, object expected)
            {
                if (!actual.Equals(expected)) throw new AssertException("Объект " + actual + " не равен объекту " + expected + "!" + Environment.NewLine);
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
}
