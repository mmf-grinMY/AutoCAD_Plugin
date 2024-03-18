using System.Threading.Tasks;
using System;
using System.Threading;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System.Windows;
using System.Collections.Concurrent;
using Autodesk.AutoCAD.Geometry;
using System.Windows.Threading;

namespace Plugins
{
    // TODO: Блокировать документ с помощью LockDocument
    public class DebugViewModel : BaseViewModel
    {
        #region Private Fields
        private int _startViewChangedCounter;
        private int _endViewChangedCounter;
        bool _isViewChanged;
        double width;

        readonly Dispatcher dispatcher;
        readonly Document doc;
        readonly CancellationTokenSource cts;
        #endregion
        public DebugViewModel(Dispatcher disp)
        {
            dispatcher = disp;
            IsViewChanged = true;
            _startViewChangedCounter = 0;
            _endViewChangedCounter = 0;
            cts = new CancellationTokenSource();
            doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            width = doc.Editor.GetCurrentView().Width;
        }
        void StartViewChanged()
        {
            IsViewChanged = false;
            ++StartViewChangedCounter;
        }
        void EndViewChanged()
        {
            IsViewChanged = true;
            ++EndViewChangedCounter;
        }
        // TODO: При повторном вызове обработки события прекращать выполнение старой обработки и начинать новое выполнение
        public async void HandleDocumentViewChanged(object sender, EventArgs args)
        {
            if (!_isViewChanged)
            {
                return;
            }

            var token = cts.Token;

            await Task.Run(() =>
            {
                StartViewChanged();
                var view = doc.Editor.GetCurrentView();

                if (Math.Abs(view.Width - width) < 0.001)
                {
                    EndViewChanged();
                    return;
                }

                var db = doc.Database;
                using (var transaction = db.TransactionManager.StartTransaction())
                {
                    var table = transaction.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    var record = transaction.GetObject(table[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                    var scale = view.Width / SystemParameters.FullPrimaryScreenWidth;

                    bool isReadEnded = false;
                    var queue = new ConcurrentQueue<Entity>();
                    int limit = 1_000;

                    Extents2d viewBound = new Extents2d
                    (
                        view.CenterPoint - (view.Height / 2.0) * Vector2d.YAxis - (view.Width / 2.0) * Vector2d.XAxis,
                        view.CenterPoint + (view.Height / 2.0) * Vector2d.YAxis + (view.Width / 2.0) * Vector2d.XAxis
                    );

                    Task.Run(() =>
                    {
                        foreach (var id in record)
                        {
                            //if (token.IsCancellationRequested)
                            //{
                            //    isReadEnded = true;
                            //    return;
                            //}

                            while (queue.Count > limit) Thread.Sleep(700);

                            var entity = transaction.GetObject(id, OpenMode.ForRead) as Entity;

                            if (entity.Bounds != null && !IsIntersecting(viewBound, entity.Bounds.Value))
                            {
                                continue;
                            }

                            switch (entity)
                            {
                                case Polyline polyline:
                                    if (polyline.LinetypeScale != scale)
                                    {
                                        queue.Enqueue(polyline);
                                    }
                                    break;
                                case Hatch hatch:
                                    var patternScale = scale * Commands.PATTERN_SCALE;
                                    if (hatch.PatternScale != patternScale)
                                    {
                                        queue.Enqueue(hatch);
                                    }
                                    break;
                                case DBText text:
                                    var textScale = scale * Commands.TEXT_SCALE;
                                    if (text.Height != textScale)
                                    {
                                        queue.Enqueue(text);
                                    }
                                    break;
                                case BlockReference reference:
                                    if (reference.ScaleFactors.X != scale)
                                    {
                                        queue.Enqueue(reference);
                                    }
                                    break;
                            }
                        }

                        isReadEnded = true;
                    }, token);

                    while (!isReadEnded || queue.Count > 0)
                    {
                        //if (token.IsCancellationRequested)
                        //{
                        //    break;
                        //}

                        if (queue.TryDequeue(out var entity))
                        {
                            switch (entity)
                            {
                                case Polyline polyline:
                                    dispatcher.Invoke(() => Update(polyline, scale));
                                    break;
                                case Hatch hatch:
                                    dispatcher.Invoke(() => Update(hatch, scale));
                                    break;
                                case DBText text:
                                    dispatcher.Invoke(() =>Update(text, scale));
                                    break;
                                case BlockReference reference:
                                    dispatcher.Invoke(() => Update(reference, scale));
                                    break;
                                default: throw new NotImplementedException("Не рассмотренный варианта блока switch!");
                            }
                        }
                    }

                    transaction.Commit();
                }

                width = view.Width;
                EndViewChanged();
            }, token);
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
            polyline.UpgradeOpen();
            polyline.LinetypeScale = scale;
        }
        void Update(BlockReference reference, double scale)
        {
            reference.UpgradeOpen();
            reference.ScaleFactors = new Scale3d(scale * 1_000, scale * 1_000, 0);
        }
        public int StartViewChangedCounter
        {
            get => _startViewChangedCounter;
            set
            {
                _startViewChangedCounter = value;
                OnPropertyChanged(nameof(StartViewChangedCounter));
            }
        }
        public int EndViewChangedCounter
        {
            get => _endViewChangedCounter;
            set
            {
                _endViewChangedCounter = value;
                OnPropertyChanged(nameof(EndViewChangedCounter));
            }
        }
        public bool IsViewChanged
        {
            get => _isViewChanged;
            set
            {
                _isViewChanged = value;
                OnPropertyChanged(nameof(IsViewChanged));
            }
        }
    }
}
