// TODO: Убрать все using у объектов, полученных через транзакцию

#define POL // Команда рисования полилинии

using Plugins.View;

using System.Collections.Generic;
using System.Windows;
using System.Text;
using System.IO;
using System;

using AApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using Point2d = Autodesk.AutoCAD.Geometry.Point2d;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

using static Plugins.Constants;

using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;




#if POL
using APolyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
#endif

namespace Plugins
{
    sealed class LineTypeNotFoundException : System.Exception { }
    public partial class Commands : IExtensionApplication
    {
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

        static DebugWindow debugWindow;

        #region Public Methods
        /// <summary>
        /// Инициализировать плагин
        /// </summary>
        public void Initialize()
        {
            var doc = AApplication.DocumentManager.MdiActiveDocument;
            //syncControl = new Control();
            if (doc is null)
            {
                MessageBox.Show("При загрузке плагина произошла ошибка!", "Ошибка", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
            {
                try
                {
                    var db = doc.Database;

                    Constants.OldWidth = doc.Editor.GetCurrentView().Width;

                    // width = doc.Editor.GetCurrentView().Width;

                    debugWindow = new DebugWindow();
                    debugWindow.Show();
                    // Task.Run(() => debugWindow.Dispatcher.Invoke(debugWindow.Show));

                    doc.ViewChanged += debugWindow.ViewModel.HandleDocumentViewChanged;
#if false
                    doc.ViewChanged += (s, e) =>
                    {
                        // TODO: Переделать первоначальную инициализацию масштабируемых параметров

                        // 1920 пикселей
                        // 24 пикселя - длина штриха
                        // 8 пикселей - длина промежутка
                        // 1 пиксель у блока - 1 единица ширины
                        // 35 пикселей - высота цифры шрифта 10

                        // 1/1920 = x/view.Width => x = view.Width/screen.Width;

                        var view = doc.Editor.GetCurrentView();
                        double scale = view.Width / Constants.OldWidth;
                        Constants.OldWidth = view.Width;
#if !true
                        var center = view.CenterPoint;

                        var min = new Point2d(center.X - view.Width / 2, center.Y - view.Height / 2);
                        var max = new Point2d(center.X + view.Width / 2, center.Y + view.Height / 2);

                        using (var transaction = db.TransactionManager.StartTransaction())
                        {
                            var table = transaction.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                            var record = transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
#if true
                            foreach (var id in record)
                            {
                                var entity = transaction.GetObject(id, OpenMode.ForRead) as Entity;

                                var bound = entity.Bounds.Value;
                                var pMin = bound.MinPoint;
                                var pMax = bound.MaxPoint;
                                if ((pMax.X < min.X) || (pMin.X > max.X) || (pMax.Y < min.Y) || (pMin.Y > max.Y))
                                {
                                    switch (entity)
                                    {
                                        case DBText text:
                                            text.Height *= scale;
                                            break;
                                        case Polyline polyline:
                                            // TODO: Сделать перезапись заливки
                                            break;
                                        case Hatch hatch:
                                            hatch.PatternScale *= scale;
                                            break;
                                        case BlockReference blockReference:
                                            // TODO: Сделать перерисовку знаков
                                            break;
                                    }
                                }
                            }
#endif

                            transaction.Commit();
                        }            
#endif
                    };
#endif

                    using (var transaction = db.TransactionManager.StartTransaction())
                    {
                        var table = transaction.GetObject(db.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;

                        if (!table.Has(TYPE_NAME))
                        {
                            db.LoadLineTypeFile(TYPE_NAME, Path.Combine(SupportPath, "acad.lin"));
                        }

                        var record = transaction.GetObject(table[TYPE_NAME], OpenMode.ForWrite) as LinetypeTableRecord;

                        transaction.Commit();
                    }

                    doc.Editor.WriteMessage("Загрузка плагина прошла успешно!");
                }
                catch (LineTypeNotFoundException)
                {
                    MessageBox.Show("Не удалось найти все стили линии!");
                    return;
                }
            }
        }
        /// <summary>
        /// Завершить работу плагина
        /// </summary>
        public void Terminate() 
        {
            File.Delete(Path.Combine(Path.GetTempPath(), DbConfigFilePath));
            File.Delete(Path.Combine(SupportPath, LineTypeLoader.STYLE_FILE));
        }
        #endregion

        #region Command Methods
        //static Control syncControl;

        /// <summary>
        /// Отрисовать геометрию
        /// </summary>
        /// <exception cref="GotoException"></exception>
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

                using (var gorizontSelecter = new View.GorizontSelecterWindow(connection.Gorizonts))
                {
                    gorizontSelecter.ShowDialog();
                    if (!gorizontSelecter.InputResult)
                    {
                        return;
                    }
                    gorizont = gorizontSelecter.Gorizont;
                }
#endif

#if OLD
                new ObjectDispatcher(connection, gorizont).Draw();
#else
                var disp = new ObjectDispatcher(connection, gorizont);
                var w = new DrawInfoWindow(disp);
                w.ShowDialog();

                // backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
                // backgroundWorker.WorkerSupportsCancellation = true;
                // backgroundWorker.RunWorkerAsync();
#endif
            }
            catch (CtorException)
            {
                return;
            }
            catch (System.Exception ex)
            {
#if !RELEASE
                MessageBox.Show($"Error: {ex.Message}\n{ex.GetType()}\n{ex.StackTrace}");
#endif
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

                    using (ExternalDbWindow window = new ExternalDbWindow(connection.GetDataTable(CreateCommand(baseName, linkField, systemId, fieldNames)).DefaultView))
                    {
                        window.Title = baseCapture;
                        window.ShowDialog();
                    }
                }
            }
        }
#endregion

#if POL
        private readonly Document doc = AApplication.DocumentManager.MdiActiveDocument;
        private readonly string TYPE_NAME = "MMP_2";

        [CommandMethod("MMP_HATCH")]
        public void DrawPolyline()
        {
            const int limit = 1; // 1_000;

            var db = doc.Database;
            var view = doc.Editor.GetCurrentView();

            var scale = view.Width / SystemParameters.FullPrimaryScreenWidth;

            using (var transaction = db.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(db.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;

                if (!table.Has(TYPE_NAME))
                {
                    db.LoadLineTypeFile(TYPE_NAME, Path.Combine(SupportPath, "acad.lin"));
                }

                transaction.Commit();
            }

            BlockTableRecord customBlockRecord;

            using (var transaction = db.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;

                customBlockRecord = new BlockTableRecord();
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
            }

            for (int i = 0; i < limit; ++i)
            {
                using (var transaction = db.TransactionManager.StartTransaction())
                {
                    var table = transaction.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                    var record = transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    var polyline = new APolyline
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

                    transaction.Commit();
                }
            }

            MessageBox.Show("Закончена отрисовка объектов!");
        }
#endif
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

                MessageBox.Show(counter.ToString());
            }
        }
        public static int PATTERN_SCALE = 10_000;
        public static int TEXT_SCALE = 8;
    }
}