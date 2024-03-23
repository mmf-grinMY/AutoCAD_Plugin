//#define FAST_MODE // Ускоренный режим работы за счет сокращения затрат на обновление UI

using Plugins.Logging;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System;

using Oracle.ManagedDataAccess.Client;

namespace Plugins.View
{
    /// <summary>
    /// Логика взаимодействия для DebugWindow.xaml
    /// </summary>
    partial class DrawInfoWindow : Window
    {
        internal DrawInfoWindow() => InitializeComponent();
    }
    class DrawInfoViewModel : BaseViewModel
    {
        #region Private Fields

        readonly uint totalCount;
        readonly ILogger logger;
        readonly Session session;
        readonly ConcurrentQueue<Entities.Primitive> queue;
        readonly BackgroundWorker readWorker;
        readonly BackgroundWorker writeWorker;

        bool isReadEnded;
        double readProgress;
        double writeProgress;
        Visibility stopVisibility;
        Visibility progressVisibility;

        #endregion

        #region Private Methods

        void StopDrawing()
        {
            writeWorker.CancelAsync();
            readWorker.CancelAsync();
        }
        void Read(object sender, DoWorkEventArgs args)
        {
#if !FAST_MODE
            uint readCount = 0;
#endif
            int limit = 1_000;
            int sleepTime = 2_000;

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
                        catch (OracleException ex)
                        {
                            if (ex.Message == "ORA-03135: Connection lost contact")
                                logger.LogError("Разорвано соединение с БД!");
                            else
                                throw;
                        }
#if !FAST_MODE
                        ReadProgress = ++readCount * 1.0 / totalCount * 100;
#endif
                    }
                }
            }
            finally
            {
                isReadEnded = true;
            }
        }
        void Write(object sender, DoWorkEventArgs args)
        {
            uint writeCount = 0;

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
#if !FAST_MODE
                    WriteProgress = ++writeCount * 1.0 / totalCount * 100;
#endif
                }
            }

            session.Window.Dispatcher.Invoke(() => session.Window.Close());
        }

#endregion

        #region Ctors

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
            //writeWorker.RunWorkerCompleted += (sender, args) =>
            //{
            //    MessageBox.Show("Вызов обработчика!");
            //    s.Window.Close();
            //};
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

        public void HandleOperationCancel(object sender, EventArgs args) => StopDrawing();

        #endregion

        public RelayCommand CancelDrawCommand { get; }
    }
}
