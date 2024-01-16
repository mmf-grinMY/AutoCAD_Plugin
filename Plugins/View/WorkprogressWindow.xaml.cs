using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Plugins.View
{
    /// <summary>
    /// Логика взаимодействия для WorkProgressWindow.xaml
    /// </summary>
    public partial class WorkProgressWindow : Window
    {
        public bool isCancelOperation = false;
        private readonly ObjectDispatcherCtorArgs args;
        public ProgressBar ProgressBar => progressBar;
        private readonly BackgroundWorker backgroundWorker;
        public WorkProgressWindow(ObjectDispatcherCtorArgs args)
        {
            InitializeComponent();

            allObjects.Text = args.Limit.ToString();
            this.args = args;

            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += Background_DoDrawObjects;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.RunWorkerAsync();
        }
        public void ReportProgress(int progress)
        {
            progressBar.Value = (progress * 1.0) / args.Limit * 100;
            currentObject.Text = progress.ToString();
        }
        private void Background_DoDrawObjects(object sender, DoWorkEventArgs e)
        {
            new ObjectDispatcher(args).Start(this);

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
