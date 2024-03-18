#define MY_BOUNDING_BOX

#define BACKGROUND_WORKER

using Plugins.Entities;

using System.Collections.Generic;
using System.Windows;
using System;

using AApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Security.Policy;
using Oracle.ManagedDataAccess.Client;




#if BACKGROUND_WORKER
using System.ComponentModel;
#endif

namespace Plugins
{
    /// <summary>
    /// Диспетчер управления отрисовкой примитивов
    /// </summary>
    class ObjectDispatcher
    {
        #region Private Fields

        private readonly EntitiesFactory factory;
        /// <summary>
        /// Рисуемый горизонт
        /// </summary>
        private readonly string gorizont;
        /// <summary>
        /// Менеджер поключения к Oracle БД
        /// </summary>
        private readonly OracleDbDispatcher connection;
        /// <summary>
        /// Внутренняя БД AutoCAD
        /// </summary>
        private static readonly Database db;
#if MY_BOUNDING_BOX
        readonly Box box;
#endif

        #endregion
        
        #region Private Methods

        /// <summary>
        /// Создать новый слой
        /// </summary>
        /// <param name="layerName">Имя слоя</param>
        public void CreateLayer(string layerName)
        {
            using (var transaction = db.TransactionManager.StartTransaction())
            {
                var table = transaction.GetObject(db.LayerTableId, OpenMode.ForWrite) as LayerTable;
                var record = new LayerTableRecord { Name = layerName };
                table.Add(record);
                transaction.AddNewlyCreatedDBObject(record, true);

                transaction.Commit();
            }
        }
        public OracleDataReader GetDrawParams()
        {
            return connection.GetDrawParams(gorizont);
        }
        public void ConnectionDispose()
        {
            connection.Dispose();
        }
        public int Count() => connection.Count(gorizont);

        public Entities.Entity Create(Primitive draw)
        {
            return factory.Create(draw);
        }

        #endregion

        #region Ctors

        /// <summary>
        /// Статическое создание
        /// </summary>
        static ObjectDispatcher()
        {
            db = AApplication.DocumentManager.MdiActiveDocument.Database;
        }
        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="connection">Менеджер подключения</param>
        /// <param name="selectedGorizont">Выбранный горизонт</param>
        public ObjectDispatcher(OracleDbDispatcher connection, string selectedGorizont)
        {
            this.connection = connection;
            gorizont = selectedGorizont;
            box = new Box() { Bottom = long.MaxValue, Left = long.MaxValue, Right = long.MinValue, Top = long.MinValue };
            factory = new EntitiesFactory(AApplication.DocumentManager.MdiActiveDocument.Database, box);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Начать отрисовку объектов
        /// </summary>
        /// <param name="window">Окно отображения пргресса отрисовки</param>
        public void Draw()
        {
            var layersCache = new HashSet<string>();

            var queue = new ConcurrentQueue<Primitive>();
#if true
            bool isEnded = false;
#endif
            Action readAction = () =>
            {
                using (var reader = connection.GetDrawParams(gorizont))
                {
                    reader.FetchSize *= 2;
                    while (reader.Read())
                    {
                        queue.Enqueue(new Primitive(reader["geowkt"].ToString(),
                                                      reader["drawjson"].ToString(),
                                                      reader["paramjson"].ToString(),
                                                      reader["layername"] + " | " + reader["sublayername"],
                                                      reader["systemid"].ToString(),
                                                      reader["basename"].ToString(),
                                                      reader["childfields"].ToString()));
                    
                    }
                }
                isEnded = true;

                MessageBox.Show("Все данные из таблицы считаны!");
            };

#if OLD
            Action drawAction = () =>
            {
                if (queue.TryDequeue(out var draw))
                {

                    var layer = draw.LayerName;

                    if (!layersCache.Contains(layer))
                    {
                        layersCache.Add(layer);
                        CreateLayer(layer);
                    }

                    using (var entity = factory.Create(draw))
                    {
                        entity?.Draw();
                    }
                }
            };
#endif
            // TODO: Нормально распараллелить
            Task.Run(() => Parallel.Invoke(readAction, () =>
            {
                while (!isEnded)
                {
                    try
                    {
                        if (queue.TryDequeue(out var draw))
                        {
                            var layer = draw.LayerName;

                            if (!layersCache.Contains(layer))
                            {
                                layersCache.Add(layer);
                                CreateLayer(layer);
                            }

                            using (var entity = factory.Create(draw))
                            {
                                entity?.Draw();
                            }
                        }
                    }
                    catch (NoDrawingLineException) { }
                    catch (FormatException) { }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.GetType() + "\n" + ex.Message + "\n" + ex.StackTrace + "\n" + ex.Source);
                    }
                }

                MessageBox.Show("Закончена отрисовка геометрии!");
                AApplication.DocumentManager.MdiActiveDocument.Editor
                    .Zoom(new Extents3d(new Point3d(box.Left, box.Bottom, 0), new Point3d(box.Right, box.Top, 0)));
            }));
#if false
            using (var reader = await connection.GetDrawParamsAsync(gorizont))
            {
                // Увеличение размера строки запроса
                reader.FetchSize *= 2;
                // TODO: Добавить индикатор прогресса
                while (reader.Read())
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            var draw = new Primitive(reader["geowkt"].ToString(),
                                                      reader["drawjson"].ToString(),
                                                      reader["paramjson"].ToString(),
                                                      reader["layername"] + " | " + reader["sublayername"],
                                                      reader["systemid"].ToString(),
                                                      reader["basename"].ToString(),
                                                      reader["childfields"].ToString());

                            var layer = draw.LayerName;

                            if (!layersCache.Contains(layer))
                            {
                                layersCache.Add(layer);
                                CreateLayer(layer);
                            }

                            using (var entity = factory.Create(draw))
                            {
                                entity?.Draw();
                            }
                        }
                        catch (NoDrawingLineException) { }
                        catch (FormatException)
                        {
                            var rows = new string[]
                            {
                            "geowkt",
                            "drawjson",
                            "paramjson",
                            "layername",
                            "sublayername",
                            "systemid",
                            "basename",
                            "childfields"
                            };

                            foreach (var row in rows)
                            {
                                if (reader[row] == null)
                                    MessageBox.Show("Столбец " + row + " принимает значение NULL!" + '\n' + reader["geowkt"]);
                            }
                        }
                        catch (Exception ex)
                        {
#if !RELEASE
                            MessageBox.Show(ex.GetType() + "\n" + ex.Message + "\n" + ex.StackTrace + "\n" + ex.Source);
#endif
                        }
                    });
                }
            }
#endif
        }
        public void Zoom()
        {
            AApplication.DocumentManager.MdiActiveDocument.Editor
                    .Zoom(new Extents3d(new Point3d(box.Left, box.Bottom, 0), new Point3d(box.Right, box.Top, 0)));
        }

#endregion
    }
    public sealed class NoDrawingLineException : Exception { }


    namespace Plugins.View
    {
        /// <summary>
        /// Логика взаимодействия для WorkProgressWindow.xaml
        /// </summary>
        public partial class WorkProgressWindow : Window
        {
            public bool isCancelOperation = false;

            // private readonly ObjectDispatcherCtorArgs args;
            // public ProgressBar ProgressBar => progressBar;
            private readonly BackgroundWorker backgroundWorker;
            public WorkProgressWindow()
            {
                //InitializeComponent();

                //allObjects.Text = args.Limit.ToString();
                // this.args = args;


                backgroundWorker = new BackgroundWorker();
                backgroundWorker.DoWork += Background_DoDrawObjects;
                backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
                backgroundWorker.WorkerSupportsCancellation = true;
                backgroundWorker.RunWorkerAsync();

            }
            public void ReportProgress(int progress)
            {
                // progressBar.Value = (progress * 1.0) / args.Limit * 100;
                // currentObject.Text = progress.ToString();
            }
            private void Background_DoDrawObjects(object sender, DoWorkEventArgs e)
            {
                // new ObjectDispatcher(args).Start(this);

                if (!isCancelOperation)
                {
                    e.Result = "Операция завершена!";
                }
                else
                {
                    e.Cancel = true;
                    this.Dispatcher.Invoke(() =>
                    {
                        this.Close();
                    });
                }
            }
            private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
            {
                if (e.Error != null)
                {
                    MessageBox.Show("Ошибка: " + e.Error.Message);
                    this.Close();
                }
                else if (!e.Cancelled)
                {
                    MessageBox.Show(e.Result.ToString());
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Операция была прервана!");
                }
            }
            private void Button_Click(object sender, RoutedEventArgs e)
            {
                isCancelOperation = true;
                backgroundWorker.CancelAsync();
            }
        }
    }
}