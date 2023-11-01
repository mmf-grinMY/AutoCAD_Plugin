using System;
using System.Threading;
using System.Collections.Generic;

namespace Plugins
{
    public class Conveyor<T>
    {
        #region Fields

        private readonly Buffer _buffer = new Buffer();
        private readonly Func< T> _read;
        private readonly Action<T> _write;

        #endregion

        #region Ctors

        public Conveyor(Func<T> read, Action<T> write)
        {
            _read = read;
            _write = write;
        }

        #endregion

        public void Run()
        {
            Thread readThread = null;
            Thread writeThread = null;
            writeThread = new Thread(new ThreadStart(() =>
            {
                if (_buffer.Count > 0)
                {
                    _write(_buffer.Remove());
                }
                else if (readThread.ThreadState == ThreadState.Aborted || readThread.ThreadState == ThreadState.Stopped)
                {
                    writeThread.Abort();
                }
            }));
            readThread = new Thread(new ThreadStart(() =>
            {
                if (_buffer.Count < _buffer.MaxLength) 
                {
                    T item = _read();
                    if (item != null)
                    {
                        _buffer.Add(item);
                    }
                    else
                    {
                        readThread.Abort();
                    }
                }
            }));
            readThread.Start();
            writeThread.Start();
            readThread.Join();
            writeThread.Join();
        }
        private class Buffer
        {
            public int MaxLength => 20;
            private readonly Stack<T> _list = new Stack<T>();
            private readonly object _locker = new object();

            public void Add(T item)
            {
                lock (_locker)
                {
                    if (_list.Count < MaxLength)
                    {
                        _list.Push(item);

                        if (_list.Count == MaxLength - 1)
                        {
                            IsFullNext = true;
                        }
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(item));
                    }
                }
            }

            public T Remove()
            {
                lock (_locker)
                {
                    if (IsFullNext)
                    {
                        IsFullNext = false;
                    }

                    return _list.Pop();
                }
            }

            public int Count => _list.Count;

            public bool IsFullNext { get; set; }
        }
    }
}
