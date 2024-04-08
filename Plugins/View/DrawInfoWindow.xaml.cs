using Plugins.Logging;

using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System;

// TODO: Исправить логику остановки отрисовки плагина
namespace Plugins.View
{
    /// <summary>
    /// Окно прогресса отрисовки
    /// </summary>
    partial class DrawInfoWindow : Window
    {
        /// <summary>
        /// Создание объекта
        /// </summary>
        internal DrawInfoWindow() => InitializeComponent();
    }
    /// <summary>
    /// Логика окна управления работой команды отрисовки
    /// </summary>
    class DrawInfoViewModel : BaseViewModel
    {
        #region Private Readonly Fields

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

        #endregion

        #region Private Fields

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
        /// <summary>
        /// Количество записанных объектов
        /// </summary>
        uint writePosition;
        /// <summary>
        /// Источник токенов отмены асинхронных операций
        /// </summary>
        CancellationTokenSource cts;
        /// <summary>
        /// Завершение операции записи на чертеж
        /// </summary>
        bool isWriteEnded;

        #endregion

        #region Public Fields

        /// <summary>
        /// Завершение операции чтения из БД Oracle
        /// </summary>
        public bool isReadEnded;
        /// <summary>
        /// Количество прочитанных объектов
        /// </summary>
        public uint readPosition;

        #endregion

        #region Private Methods

        /// <summary>
        /// Отмена операции отрисовки
        /// </summary>
        void CancelDrawing()
        {
            session.WriteMessage("Работа команды " + Commands.DRAW_COMMAND + (isWriteEnded ? " успешно завершена!" : " была прервана!"));
            StopDrawing();
            logger.LogInformation("Текущая позиция чтения " + readPosition.ToString());
            logger.LogInformation("Текущая позиция записи " + writePosition.ToString());
            session.Close();
        }
        /// <summary>
        /// Логика остановки работы
        /// </summary>
        void StopDrawing()
        {
            cts.Cancel();
            ProgressVisibility = Visibility.Collapsed;
            IsStopedVisibility = Visibility.Visible;
        }
        /// <summary>
        /// Запись примитивов на чертеж
        /// </summary>
        /// <param name="token">Токен отмены асинхронной операции</param>
        async void WriteAsync(CancellationToken token)
        {
            await Task.Run(() =>
            {
                uint percent = 0;

                while (!isReadEnded || queue.Count > 0)
                {
                    if (token.IsCancellationRequested) return;

                    if (queue.TryDequeue(out var draw))
                    {
                        session.Add(draw);

                        uint currentPrecent = ++writePosition * 100 / totalCount;

                        if (currentPrecent > percent)
                        {
                            percent = currentPrecent;
                            WriteProgress = percent;
                        }
                    }
                }
                isWriteEnded = true;
                session.Close();
            }, token);
        }
        /// <summary>
        /// Работа команды отрисовки
        /// </summary>
        void RunAsync()
        {
            ProgressVisibility = Visibility.Visible;
            IsStopedVisibility = Visibility.Collapsed;
            cts?.Dispose();
            cts = new CancellationTokenSource();
            connection.ReadAsync(cts.Token, queue, this, session);
            WriteAsync(cts.Token);
        }

        #endregion

        #region Ctor

        /// <summary>
        /// Создание объекта
        /// </summary>
        /// <param name="session">Текущая сессия работы</param>
        /// <param name="logger">Логер событий</param>
        public DrawInfoViewModel(Session session, ILogger logger, IDbDispatcher dispatcher)
        {
            this.logger = logger;
            this.session = session;
            connection = dispatcher;

            totalCount = this.session.PrimitivesCount;
            if (totalCount == 0)
                throw new ArgumentOutOfRangeException(nameof(totalCount));

            CancelCommand = new RelayCommand(obj => CancelDrawing());
            StopCommand = new RelayCommand(obj => StopDrawing());
            ContinueCommand = new RelayCommand(obj => RunAsync());

            ProgressVisibility = Visibility.Visible;
            IsStopedVisibility = Visibility.Collapsed;
            readPosition = 0;
            writePosition = 0;
            readProgress = 0;
            writeProgress = 0;
            isReadEnded = false;
            isWriteEnded = false;
            queue = new ConcurrentQueue<Entities.Primitive>();

            RunAsync();

            this.logger.LogInformation("Запущена отрисовка геометрии!");
        }
        readonly IDbDispatcher connection;

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
        /// Обработка экстренной остановки работы плагина
        /// </summary>
        /// <param name="sender">Вызывающий объект</param>
        /// <param name="args">Параметры события</param>
        public void HandleOperationCancel(object sender, EventArgs args) => CancelDrawing();

        #endregion

        #region Commands

        /// <summary>
        /// Команда отмены работы
        /// </summary>
        public RelayCommand CancelCommand { get; }
        /// <summary>
        /// Команда установки паузы
        /// </summary>
        public RelayCommand StopCommand { get; }
        /// <summary>
        /// Команда продолжения работы
        /// </summary>
        public RelayCommand ContinueCommand { get; }

        #endregion
    }
}
