//#define FAST_MODE // Ускоренный режим работы за счет сокращения затрат на обновление UI

using Plugins.Logging;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.IO;
using System;

using Oracle.ManagedDataAccess.Client;

using Newtonsoft.Json.Linq;

namespace Plugins.View
{
    /// <summary>
    /// Монтор прогресса отрисовки
    /// </summary>
    partial class DrawInfoWindow : Window
    {
        /// <summary>
        /// Создание объекта
        /// </summary>
        internal DrawInfoWindow() => InitializeComponent();
    }
    /// <summary>
    /// Логика взаимодействия для DrawInfoWindow
    /// </summary>
    class DrawInfoViewModel : BaseViewModel
    {
        #region Private Fields

        /// <summary>
        /// Общее количество доступных для отрисовки объектов
        /// </summary>
        readonly uint totalCount;
        /// <summary>
        /// Логер событий
        /// </summary>
        readonly ILogger logger;
        /// <summary>
        /// Текущая сессия работы плагина
        /// </summary>
        readonly Session session;
        /// <summary>
        /// Очередь доступных для записи объектов
        /// </summary>
        readonly ConcurrentQueue<Entities.Primitive> queue;
        /// <summary>
        /// Фоновое чтение объектов из БД
        /// </summary>
        readonly BackgroundWorker readWorker;
        /// <summary>
        /// Фоновая запись объектов на чертеж AutoCAD
        /// </summary>
        readonly BackgroundWorker writeWorker;

        /// <summary>
        /// Все объекты прочитаны
        /// </summary>
        bool isReadEnded;
        /// <summary>
        /// Прогресс чтения объектов
        /// </summary>
        double readProgress;
        /// <summary>
        /// Прогресс записи объектов
        /// </summary>
        double writeProgress;
        /// <summary>
        /// Видимость блока остановки работы
        /// </summary>
        Visibility stopVisibility;
        /// <summary>
        /// Видимость блока прогресса работы
        /// </summary>
        Visibility progressVisibility;

        #endregion

        #region Private Methods

        /// <summary>
        /// Логика остановки работы
        /// </summary>
        void StopDrawing()
        {
            writeWorker.CancelAsync();
            readWorker.CancelAsync();
        }
        /// <summary>
        /// Чтение данных из БД
        /// </summary>
        /// <param name="sender">Вызывающий событие объект</param>
        /// <param name="args">Параметры события</param>
        void Read(object sender, DoWorkEventArgs args)
        {
            uint precent = 0;
#if !FAST_MODE
            uint readCount = 0;
#endif
            var config = JObject.Parse(File.ReadAllText(Path.Combine(Constants.SupportPath, Constants.CONFIG_FILE))).Value<JObject>("queue");

            var limit = config.Value<int>("limit");
            var sleepTime = config.Value<int>("sleep");

            try
            {
                using (var reader = session.DrawDataReader)
                {
                    reader.FetchSize = 2;
                    while (reader.Read())
                    {
                        if (readWorker.CancellationPending)
                        {
                            args.Cancel = true;
                            return;
                        }

                        while (queue.Count > limit) Thread.Sleep(sleepTime);

                        try
                        {
                            queue.Enqueue(new Entities.Primitive(reader["geowkt"].ToString(),
                                                          reader["drawjson"].ToString(),
                                                          reader["paramjson"].ToString(),
                                                          reader["layername"] + " | " + reader["sublayername"],
                                                          reader["systemid"].ToString(),
                                                          reader["basename"].ToString(),
                                                          reader["childfields"].ToString()));
                        }
                        catch (OracleException e)
                        {
                            if (e.Message == "ORA-03135: Connection lost contact")
                                logger.LogError("Разорвано соединение с БД!");
                            else
                                throw;
                        }
                        catch (InvalidOperationException e)
                        {
                            if (e.Message != "Invalid operation on a closed object")
                                throw;
                            else
                                MessageBox.Show("Из-за сбоя подключение к БД Oracle прервано!");
                        }

                        uint currentPrecent = ++readCount * 100 / totalCount;

                        if (currentPrecent > precent)
                        {
                            precent = currentPrecent;
#if !FAST_MODE
                            ReadProgress = precent;
#endif
                        }
                    }
                }
            }
            finally
            {
                isReadEnded = true;
            }
        }
        /// <summary>
        /// Логика записи объектов на чертеж
        /// </summary>
        /// <param name="sender">Вызывающий объект</param>
        /// <param name="args">Параметры события</param>
        void Write(object sender, DoWorkEventArgs args)
        {
            uint writeCount = 0;
            uint precent = 0;

            while (!isReadEnded || queue.Count > 0)
            {
                if (writeWorker.CancellationPending)
                {
                    args.Cancel = true;
                    ProgressVisibility = Visibility.Collapsed;
                    IsStopedVisibility = Visibility.Visible;

                    return;
                }

                if (queue.TryDequeue(out var draw))
                {
                    session.Add(draw);

                    uint currentPrecent = ++writeCount * 100 / totalCount;

                    if (currentPrecent > precent)
                    {
                        precent = currentPrecent;
#if !FAST_MODE
                        WriteProgress = precent;
#endif
                    }
                }
            }

            session.Window.Dispatcher.Invoke(() => session.Window.Close());
        }

#endregion

        #region Ctors

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="s">Текущая сессия работы</param>
        /// <param name="log">Логер событий</param>
        public DrawInfoViewModel(Session s, ILogger log)
        {
            logger = log;
            session = s;
            CancelDrawCommand = new RelayCommand(obj => StopDrawing());

            ProgressVisibility = Visibility.Visible;
            IsStopedVisibility = Visibility.Collapsed;

            readProgress = 0;
            writeProgress = 0;

            queue = new ConcurrentQueue<Entities.Primitive>();
#if !FAST_MODE
            totalCount = session.PrimitivesCount;
#endif
            isReadEnded = false;

            readWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            readWorker.DoWork += Read;
            readWorker.RunWorkerCompleted += (sender, args) => isReadEnded = true;
            readWorker.RunWorkerAsync();

            writeWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            writeWorker.DoWork += Write;
            writeWorker.RunWorkerAsync();

            logger.LogInformation("Запущена отрисовка геометрии!");
        }

        #endregion

        #region Binding Properties

        public double ReadProgress
        {
            get => readProgress;
            set
            {
                readProgress = value;
                OnPropertyChanged(nameof(ReadProgress));
                OnPropertyChanged(nameof(QueueCount));
            }
        }
        public double WriteProgress
        {
            get => writeProgress;
            set
            {
                writeProgress = value;
                OnPropertyChanged(nameof(WriteProgress));
                OnPropertyChanged(nameof(QueueCount));
            }
        }
        public int QueueCount => queue.Count;
        public Visibility IsStopedVisibility
        {
            get => stopVisibility;
            set
            {
                stopVisibility = value;
                OnPropertyChanged(nameof(IsStopedVisibility));
            }
        }
        public Visibility ProgressVisibility
        {
            get => progressVisibility;
            set
            {
                progressVisibility = value;
                OnPropertyChanged(nameof(ProgressVisibility));
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Обработка экстренной остановки рабоыт плагина
        /// </summary>
        /// <param name="sender">Вызывающий объект</param>
        /// <param name="args">Параметры события</param>
        public void HandleOperationCancel(object sender, EventArgs args) => StopDrawing();

        #endregion

        /// <summary>
        /// Команда отмены работы
        /// </summary>
        public RelayCommand CancelDrawCommand { get; }
    }
}
