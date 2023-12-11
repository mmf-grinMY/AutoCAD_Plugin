#region Usings

using System;
using System.Collections.Generic;
using System.Threading;

#endregion

namespace Plugins
{
    internal class Pipeline<T>
    {
        private readonly Func<T> _read;
        private readonly Action<T> _write;
        private readonly int _maxLength;
        private readonly object _locker = new object();
        private readonly List<T> _list;
        private readonly Thread _readThread;
        private readonly Thread _writeThread;
        private readonly int _limitItemsCount;
        private int _counter = 0;
        public int ReadedItemsCount => _counter;
        public Pipeline(Func<T> read, Action<T> write, int bufferSize = 500, int limitItemsCount = 10_000)
        {
            _read = read;
            _write = write;
            _maxLength = bufferSize;
            _limitItemsCount = limitItemsCount;
            _list = new List<T>();
            _readThread = new Thread(() =>
            {
                while (_readThread.IsAlive)
                {
                    if (_list.Count < _maxLength)
                    {
                        object item = null;
                        lock (_locker)
                        {
                            item = _read();
                        }
                        _counter++;
                        if (item != null && _counter < _limitItemsCount)
                        {
                            _list.Add((T)item);
                        }
                        else
                        {
                            _readThread.Abort();
                        }
                    }
                }
            });
            _writeThread = new Thread(() =>
            {
                while (_writeThread.IsAlive)
                {
                    if (_list.Count > 0)
                    {
                        T item;
                        lock (_locker)
                        {
                            item = _list[0];
                            _list.RemoveAt(0);
                        }
                        _write(item);
                    }
                    else if (!_readThread.IsAlive)
                    {
                        _writeThread.Abort();
                    }
                }
            });
        }
        public void Run()
        {
            _readThread.Start();
            _writeThread.Start();
            _readThread.Join();
            _writeThread.Join();
        }
    }
}