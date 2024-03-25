// TODO: Все ошибки логировать в отдельный файл, а не в MessageBox

// TODO: Добавить в окно мониторинга за процессом отрисовки график заполнения очереди

// TODO: Добавить фильтр для выборки только определенных слоев

// TODO: Добавить панель со слоями как в MapManager

// TODO: Сделать нормальное масштабирование

// TODO: Сделать ограничение на отрисовку в одном чертеже только одного горизонта

// TODO: Добавить описание выбрасываемых исключений ко всем методам

#define POL // Команда рисования полилинии

using Plugins.Logging;
using Plugins.View;

using System.Collections.Generic;
using System.Windows;
using System.Text;
using System.IO;
using System;

using AApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

using static Plugins.Constants;

#if POL
using Point2d = Autodesk.AutoCAD.Geometry.Point2d;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
#endif

namespace Plugins
{
    public class Commands : IExtensionApplication
    {
        #region Private Fields

        // TODO: Добавить настройку имени штриховки линии и файла источника из конфигурационного файла
        readonly Document doc = AApplication.DocumentManager.MdiActiveDocument;
        readonly string LINE_TYPE_SOURCE = "acad.lin";
        readonly string TYPE_NAME = "MMP_2";
        static double width;
        static ILogger logger;

        #endregion

        #region Private Methods

        /// <summary>
        /// Создание команды выборки данных
        /// </summary>
        /// <param name="baseName">Имя линкованной таблицы</param>
        /// <param name="linkField">Столбец линковки</param>
        /// <param name="systemId">Уникальный номер примитива</param>
        /// <param name="fieldNames">Список столбцов таблицы</param>
        /// <returns>Команда для получения данных</returns>
        private string CreateCommand(string baseName, string linkField, int systemId, IDictionary<string, string> fieldNames)
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
        private Dictionary<string, string> ParseFieldNames(IEnumerable<string> fields)
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

        #endregion

        #region Public Methods

        /// <summary>
        /// Инициализировать плагин
        /// </summary>
        public void Initialize()
        {
            if (doc is null)
            {
                MessageBox.Show("Невозможен доступ к активному документу!", "Ошибка", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var view = doc.Editor.GetCurrentView()) { width = view.Width; }

            logger = SessionDispatcher.Logger;

            try
            {
                doc.Database.LoadLineTypeFile(TYPE_NAME, LINE_TYPE_SOURCE);
                doc.Editor.WriteMessage("Загрузка плагина прошла успешно!");
            }
            catch (Autodesk.AutoCAD.Runtime.Exception e)
            {
                if (e.Message == "eUndefinedLineType")
                {
                    doc.Editor.WriteMessage("Не удалось найти стиль линии \"" + TYPE_NAME + "\" в файле \"" + LINE_TYPE_SOURCE + "\"!");
                }
                else
                {
                    throw;
                }
            }
        }
        /// <summary>
        /// Завершить работу плагина
        /// </summary>
        public void Terminate() => File.Delete(Path.Combine(Path.GetTempPath(), DbConfigFilePath));

        #endregion

        #region Command Methods

        /// <summary>
        /// Отрисовать геометрию
        /// </summary>
        [CommandMethod("MMP_DRAW")]
        public void DrawCommand()
        {
            // TODO: Добавить поддержку пользовательского BoundingBox
            OracleDbDispatcher connection = null;
            try
            {
#if DEBUG
                connection = new OracleDbDispatcher("Data Source=data-pc/GEO;Password=g1;User Id=g;Connection Timeout=360;");
                string gorizont = "K450E";
#else
                if (!OracleDbDispatcher.TryGetConnection(out connection)) return;

                string gorizont;

                var gorizontSelecter = new GorizontSelecterWindow(connection.Gorizonts);
                gorizontSelecter.ShowDialog();
                if (!gorizontSelecter.InputResult)
                {
                    return;
                }
                gorizont = gorizontSelecter.Gorizont;
                gorizontSelecter.Close();
#endif
                SessionDispatcher.StartSession(connection, gorizont);
                var finish = "Закончена отрисовка геометрии!";
                logger.LogInformation(finish);
                doc.Editor.WriteMessage(finish);
            }
            catch (TypeInitializationException)
            {
                logger.LogWarning("Не удалось подключиться к БД!");
                return;
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
            }
            finally
            {
                connection.Dispose();
            }
        }
        /// <summary>
        /// Команда инспектирования отрисованных примитивов
        /// </summary>
        [CommandMethod("VRM_INSPECT_EXT_DB")]
        public void InspectExtDB()
        {
            if (!OracleDbDispatcher.TryGetConnection(out OracleDbDispatcher connection)) return;

            var document = AApplication.DocumentManager.MdiActiveDocument;
            var editor = document.Editor;
            var options = new PromptEntityOptions("\nВыберите объект: ");
            using (var transaction = document.TransactionManager.StartTransaction())
            {
                PromptEntityResult result = null;
                while ((result = editor.GetEntity(options)).Status == PromptStatus.OK)
                {
                    var buffer = transaction.GetObject(result.ObjectId, OpenMode.ForRead).XData;
                    if (buffer == null)
                    {
                        editor.WriteMessage("\nУ объекта отсутствуют параметры XData");
                        continue;
                    }

                    string xData;
                    const int ERROR_SYSTEM_ID = -1;

                    int systemId;
                    string[] row;
                    string linkField;

                    if ((linkField = buffer.GetXData(LINK_FIELD)) == string.Empty
                        || (systemId = (xData = buffer.GetXData(SYSTEM_ID)) == string.Empty ? ERROR_SYSTEM_ID : Convert.ToInt32(xData)) == ERROR_SYSTEM_ID
                        || (xData = buffer.GetXData(BASE_NAME)) == string.Empty
                        || (row = xData.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)).Length <= 1)
                    {
                        editor.WriteMessage("Атрибутивная таблица к объекту отсутствует!");
                        continue;
                    }
                        
                    string baseName = row[1];
                    string baseCapture = baseName;
                    var link = connection.GetExternalDbLink(baseName);

                    var fields = link.Split('\n');
                    var fieldNames = new Dictionary<string, string>();

                    if (fields.Length > 5) fieldNames = ParseFieldNames(fields);

                    if (fields.Length > 2) baseCapture = fields[1];

                    new ExternalDbWindow(connection.GetDataTable(CreateCommand(baseName, linkField, systemId, fieldNames)).DefaultView)
                    {
                        Title = baseCapture
                    }.ShowDialog();
                }
            }
        }
        #endregion

#if POL
        BlockTableRecord InitializeCustomBlock()
        {
            var db = doc.Database;

            using (var transaction = db.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;

                var customBlockRecord = new BlockTableRecord();
                table.Add(customBlockRecord);
                customBlockRecord.Name = "CustomBlock";
                transaction.AddNewlyCreatedDBObject(customBlockRecord, true);

                var circle = new Circle
                {
                    Center = new Point3d(0, 0, 0),
                    Radius = 3,
                    Color = Color.FromColorIndex(ColorMethod.ByBlock, 0)
                };

                customBlockRecord.AppendEntity(circle);
                transaction.AddNewlyCreatedDBObject(circle, true);

                transaction.Commit();

                return customBlockRecord;
            }
        }
        [CommandMethod("MMP_TEST_DRAW")]
        public void DrawPolyline()
        {
            const int limit = 1;

            var db = doc.Database;
            var view = doc.Editor.GetCurrentView();

            var scale = view.Width / SystemParameters.FullPrimaryScreenWidth;

            var customBlockRecord = InitializeCustomBlock();

            using (var transaction = db.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                var record = transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                for (int i = 0; i < limit; ++i)
                {
                    var polyline = new Polyline
                    {
                        Color = Autodesk.AutoCAD.Colors.Color.FromRgb(255, 0, 0),
                        Linetype = TYPE_NAME
                    };

                    polyline.AddVertexAt(0, new Point2d(2, 2), 0, 0, 0);
                    polyline.AddVertexAt(1, new Point2d(2, 10), 0, 0, 0);
                    polyline.AddVertexAt(2, new Point2d(10, 10), 0, 0, 0);
                    polyline.AddVertexAt(3, new Point2d(10, 2), 0, 0, 0);
                    polyline.AddVertexAt(4, new Point2d(2, 2), 0, 0, 0);

                    record.AppendEntity(polyline);
                    transaction.AddNewlyCreatedDBObject(polyline, true);

                    var hatch = new Hatch
                    {
                        PatternScale = scale * 1_000,
                        Color = Color.FromRgb(0, 255, 0),
                    };

                    record.AppendEntity(hatch);
                    transaction.AddNewlyCreatedDBObject(hatch, true);

                    hatch.SetHatchPattern(HatchPatternType.PreDefined, "DRO32!3");
                    hatch.Associative = true;
                    hatch.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection() { polyline.ObjectId });
                    hatch.EvaluateHatch(true);

                    var text = new DBText()
                    {
                        TextString = "Строка для теста!",
                        Position = new Point3d(10, 12, 0),
                        Color = Color.FromRgb(0, 0, 255),
                        Height = scale
                    };

                    record.AppendEntity(text);
                    transaction.AddNewlyCreatedDBObject(text, true);

                    var reference = new BlockReference(new Point3d(0, 0, 0), customBlockRecord.ObjectId)
                    {
                        Color = Color.FromRgb(0, 255, 255)
                    };

                    record.AppendEntity(reference);
                    transaction.AddNewlyCreatedDBObject(reference, true);
                }

                transaction.Commit();
            }

            doc.Editor.WriteMessage("Закончена отрисовка объектов!");
        }
        [CommandMethod("MMP_COUNT")]
        public void GetCount()
        {
            var db = doc.Database;

            using (var transaction = db.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                var record = transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                int counter = 0;

                foreach (var id in record)
                {
                    ++counter;
                }

                doc.Editor.WriteMessage(counter.ToString());
            }
        }
        public static int PATTERN_SCALE = 10_000;
        public static int TEXT_SCALE = 8;
        void ScaleLogic(Extents2d viewBound, double scale)
        {
            var db = doc.Database;

            using (var transaction = db.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                var record = transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                foreach (var id in record)
                {
                    Entity entity = transaction.GetObject(id, OpenMode.ForRead) as Entity;

                    if (entity.Bounds != null && !IsIntersecting(viewBound, entity.Bounds.Value))
                    {
                        return;
                    }

                    try
                    {
                        switch (entity)
                        {
                            case Polyline polyline:
                                if (polyline.LinetypeScale != scale)
                                {
                                    Update(polyline, scale);
                                    logger.LogInformation("Произошла перерисовка {0}", typeof(Polyline));
                                }
                                break;
                            case Hatch hatch:
                                var patternScale = scale * Commands.PATTERN_SCALE;
                                if (hatch.PatternScale != patternScale)
                                {
                                    Update(hatch, scale);
                                    logger.LogInformation("Произошла перерисовка {0}", typeof(Hatch));
                                }
                                break;
                            case DBText text:
                                var textScale = scale * Commands.TEXT_SCALE;
                                if (text.Height != textScale)
                                {
                                    Update(text, scale);
                                    logger.LogInformation("Произошла перерисовка {0}", typeof(DBText));
                                }
                                break;
                            case BlockReference reference:
                                if (reference.ScaleFactors.X != scale)
                                {
                                    // TODO: Переделать перерисовку блока
                                    Update(reference, scale);
                                    logger.LogInformation("Произошла перерисовка {0}", typeof(BlockReference));
                                }
                                break;
                            default:
                                {
                                    logger.LogWarning("Не обработан объект типа {0}", entity.GetType());
                                }
                                break;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        logger.LogError(ex);
                    }
                }

                transaction.Commit();
            }
        }
        // Линия и заливка, которые частично находятся на видимой области чертежа не масштабируются
        // Блок вообще не участвует в масштабировании
        const string _SCALE = "MMP_SCALE";
        [CommandMethod(_SCALE)]
        public void ViewScale()
        {
            ViewTableRecord view = null;
            try
            {
                logger.LogInformation("Запущена команда {0}", _SCALE);
                view = doc.Editor.GetCurrentView();

                if (Math.Abs(view.Width - width) < 0.001) return;

                logger.LogInformation("Запущена процедура масштабирования!");

                var scale = view.Width / SystemParameters.FullPrimaryScreenWidth;

                var viewBound = new Extents2d
                (
                    view.CenterPoint - (view.Height / 2.0) * Vector2d.YAxis - (view.Width / 2.0) * Vector2d.XAxis,
                    view.CenterPoint + (view.Height / 2.0) * Vector2d.YAxis + (view.Width / 2.0) * Vector2d.XAxis
                );

                ScaleLogic(viewBound, scale);
                doc.Editor.WriteMessage("Процедура масштабирования завершена!");
                AApplication.UpdateScreen();
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex);
            }
            finally
            {
                width = view.Width;
                view.Dispose();
                logger.LogInformation("Завершена команда {0}{1}", _SCALE, Environment.NewLine);
            }
        }
        bool IsIntersecting(Extents2d rect1, Extents3d rect2) =>
           !(rect1.MaxPoint.X < rect2.MinPoint.X || rect1.MinPoint.X > rect2.MaxPoint.X
           || rect1.MaxPoint.Y < rect2.MinPoint.Y || rect1.MinPoint.Y > rect2.MaxPoint.Y);
        void Update(DBText text, double scale)
        {
            text.UpgradeOpen();
            text.Height = scale * Commands.TEXT_SCALE;
        }
        void Update(Hatch hatch, double scale)
        {
            hatch.UpgradeOpen();
            hatch.PatternScale = scale * Commands.PATTERN_SCALE;
            hatch.SetHatchPattern(HatchPatternType.PreDefined, hatch.PatternName);
            hatch.EvaluateHatch(true);
        }
        void Update(Polyline polyline, double scale)
        {
            string log = "Изменен масштаб штриховки с " + polyline.LinetypeScale.ToString();
            polyline.UpgradeOpen();
            polyline.LinetypeScale = scale;
            log += " на " + polyline.LinetypeScale.ToString();
            logger.LogInformation(log);
        }
        void Update(BlockReference reference, double scale)
        {
            reference.UpgradeOpen();
            reference.ScaleFactors = new Scale3d(scale, scale, 0);
        }
#endif
    }
    sealed class NotDrawingLineException : System.Exception { }
}