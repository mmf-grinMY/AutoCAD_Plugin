using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Windows;
using System.Threading;

namespace Plugins
{
    /// <summary>
    /// Логика взаимодействия для DebugWindow.xaml
    /// </summary>
    partial class DrawInfoWindow : Window
    {
        private readonly BackgroundWorker readWorker;
        private readonly BackgroundWorker writeWorker;
        internal DrawInfoWindow(ObjectDispatcher disp)
        {
            InitializeComponent();
            
            var queue = new ConcurrentQueue<Primitive>();
            readWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };
            writeWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };
            bool isReadEnded = false;

            int limit = 1_000;

            int readCount = 0;
            int writeCount = 0;

            readWorker.DoWork += (sender, args) =>
            {
                try
                {
                    using (var reader = disp.GetDrawParams())
                    {
                        while (reader.Read())
                        {
                            if (readWorker.CancellationPending)
                            {
                                args.Cancel = true;
                                return;
                            }

                            while (queue.Count > limit)
                            {
                                Thread.Sleep(750);
                            }

                            ++readCount;
                            Dispatcher.Invoke(() => read.Text = readCount.ToString());
                            queue.Enqueue(new Primitive(reader["geowkt"].ToString(),
                                                          reader["drawjson"].ToString(),
                                                          reader["paramjson"].ToString(),
                                                          reader["layername"] + " | " + reader["sublayername"],
                                                          reader["systemid"].ToString(),
                                                          reader["basename"].ToString(),
                                                          reader["childfields"].ToString()));
                            Dispatcher.Invoke(() => this.queue.Text = queue.Count.ToString());
                        }
                    }
                }
                finally
                {
                    disp.ConnectionDispose();
                }
            };

            readWorker.RunWorkerCompleted += (sender, args) =>
            {
                isReadEnded = true;
            };

            readWorker.RunWorkerAsync();

            var layersCache = new HashSet<string>();

            writeWorker.DoWork += (sender, args) =>
            {
                Primitive draw = null;

                while (!isReadEnded || queue.Count > 0) 
                { 
                    if (writeWorker.CancellationPending)
                    {
                        args.Cancel = true;
                        Dispatcher.Invoke(() =>
                        {
                            progress.Visibility = Visibility.Collapsed;
                            finish.Text = "Отрисовка прекращена!";
                            finish.Visibility = Visibility.Visible;
                        });
                        return;
                    }

                    try
                    {
                        if (queue.TryDequeue(out draw))
                        {
                            ++writeCount;
                            Dispatcher.Invoke(() => 
                            { 
                                write.Text = writeCount.ToString();
                                this.queue.Text = queue.Count.ToString();
                            });

                            if (queue.Count == 0 && isReadEnded)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    progress.Visibility = Visibility.Collapsed;
                                    finish.Text = "Отрисовка успешно завершена!";
                                    finish.Visibility = Visibility.Visible;
                                    cancel.Visibility = Visibility.Collapsed;
                                });
                            }

                            var layer = draw.LayerName;
                            if (!layersCache.Contains(layer))
                            {
                                layersCache.Add(layer);
                                Dispatcher.Invoke(() => disp.CreateLayer(layer));
                            }

                            using (var entity = disp.Create(draw))
                            {
                                Dispatcher.Invoke(() => entity?.Draw());
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
            };

            writeWorker.RunWorkerCompleted += (sender, args) =>
            {
                

                disp.Zoom();
            };

            writeWorker.RunWorkerAsync();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            writeWorker.CancelAsync();
            readWorker.CancelAsync();
        }
    }
}
