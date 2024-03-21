using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System;

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

        readonly Session session;
        readonly uint totalCount;
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
            uint readCount = 0;
            int limit = 1_000;
            int sleepTime = 3_000;

            try
            {
                using (var reader = session.DrawDataReader)
                {
                    reader.FetchSize *= 2;
                    while (reader.Read())
                    {
                        if (readWorker.CancellationPending)
                        {
                            args.Cancel = true;
                            return;
                        }

                        while (queue.Count > limit) Thread.Sleep(sleepTime);

                        queue.Enqueue(new Entities.Primitive(reader["geowkt"].ToString(),
                                                      reader["drawjson"].ToString(),
                                                      reader["paramjson"].ToString(),
                                                      reader["layername"] + " | " + reader["sublayername"],
                                                      reader["systemid"].ToString(),
                                                      reader["basename"].ToString(),
                                                      reader["childfields"].ToString()));
                        ReadProgress = ++readCount * 1.0 / totalCount * 100;
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
                    try
                    {
                        session.TryAdd(draw);

                        WriteProgress = ++writeCount * 1.0 / totalCount * 100;
                    }
                    catch (NotDrawingLineException) { }
                    catch (FormatException) { }
                    catch (Exception ex)
                    {
                        // TODO: Переделать вывод ошибок в логгирование
                        MessageBox.Show(ex.GetType() + "\n" + ex.Message + "\n" + ex.StackTrace + "\n" + ex.Source);
                    }
                }
            }
        }

        #endregion

        #region Ctors

        public DrawInfoViewModel(Session s)
        {
            session = s;
            CancelDrawCommand = new RelayCommand(obj => StopDrawing());

            ProgressVisibility = Visibility.Visible;
            IsStopedVisibility = Visibility.Collapsed;

            readProgress = 0;
            writeProgress = 0;

            queue = new ConcurrentQueue<Entities.Primitive>();
            totalCount = session.PrimitivesCount;
            isReadEnded = false;

            readWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            readWorker.DoWork += Read;
            readWorker.RunWorkerCompleted += (sender, args) => isReadEnded = true;
            readWorker.RunWorkerAsync();

            writeWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            writeWorker.DoWork += Write;
            writeWorker.RunWorkerAsync();
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
