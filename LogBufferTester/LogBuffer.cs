using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MutexAndOSHandleTest;

namespace LogBufferTester
{
    public sealed class LogBuffer : IDisposable
    {
        private sealed class messageWithFlag
        {
            public string Message { get; set; }
            public  bool IsRecorded { get; set; }
        }

        private const string DefaultPath = "mylog.txt";
        private const int DefaultDelay = 50;
        private const int DefaultMessageLimit = 3;

        private readonly string _destPath;
        private readonly ConcurrentQueue<string> _messageBuffer = new ConcurrentQueue<string>();
        private readonly List<messageWithFlag> _messageJournal = new List<messageWithFlag>();

        private bool _disposed = false;
        private bool _isLogging = false;
        private StreamWriter _streamWriter;

        public bool IsLogging
        {
            get => _isLogging;
            set
            {
                _isLogging = value;
                if (_messageBuffer.Count >= MessageLimit && _isLogging)
                {
                    Flush();
                }
            }
        }

        public int Delay { get; set; } = DefaultDelay;
        public int MessageLimit { get; set; } = DefaultMessageLimit;
        private readonly MyMutex _mutex = new MyMutex();

        public LogBuffer()
        {
            _destPath = DefaultPath;
            InitializeLogBuffer();
        }

        public LogBuffer(string destPath)
        {
            this._destPath = destPath;
            InitializeLogBuffer();
        }

        public void Add(string item)
        {
            if (!_disposed)
            {
                var message = $"[{DateTime.Now}]: {item}";
                _messageBuffer.Enqueue(message);
                _messageJournal.Add(new messageWithFlag(){Message = message, IsRecorded = false});
                if (_messageBuffer.Count >= MessageLimit && IsLogging)
                {
                    Flush();
                }
            }
            else
            {
                throw new ObjectDisposedException("object was disposed");
            }
        }

        public async void AddAsync(string item)
        {
            await Task.Run(()=>Add(item));
        }
        
        private void InitializeLogBuffer()
        {
            _streamWriter = new StreamWriter(_destPath, true);
            new Thread(LoggerThreadMethod).Start();
            IsLogging = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                Flush();
            }

            _streamWriter.Flush();
            _streamWriter.Close();
            _streamWriter.Dispose();
            IsLogging = false;
            _disposed = true;
        }

        ~LogBuffer()
        {
            Dispose(false);
        }

        public void Flush()
        {
            IsLogging = false;
            _mutex.Lock();
            while (_messageBuffer.TryDequeue(out var message))
            {
                try
                { 
                    _streamWriter.WriteLine(message);
                }
                catch
                {
                    Dispose(false);
                    throw;
                }

                _streamWriter.Flush();
                _messageJournal[
                        _messageJournal.IndexOf(_messageJournal.FirstOrDefault(x => x.Message == message))]
                    .IsRecorded = true;
            }

            _mutex.Unlock();
            IsLogging = true;
        }

        public async void FlushAsync()
        {
            await Task.Run(Flush);
        }
        private void LoggerThreadMethod()
        {
            var needAFlush = false;
            while (!_disposed)
            {
                while (!_disposed && IsLogging)
                {
                    while (IsLogging && !_disposed && _messageBuffer.TryDequeue(out var message))
                    {
                        _mutex.Lock();
                        needAFlush = true;
                        try
                        {
                            _streamWriter.WriteLine(message);
                        }
                        catch
                        {
                            Dispose(false);
                            throw;
                        }

                        _messageJournal[
                                _messageJournal.IndexOf(_messageJournal.FirstOrDefault(x =>
                                    x.Message == message && x.IsRecorded == false))]
                            .IsRecorded = true;
                        _mutex.Unlock();
                    }

                    if (needAFlush)
                    {
                        _streamWriter.Flush();
                        needAFlush = false;
                    }

                    Sleep();
                }

                Thread.Yield();
            }
        }

        private void Sleep()
        {
            var delay = Delay;
            if (delay > 1000)
            {
                var tmpDelay = delay;
                var surplus = tmpDelay % 1000;
                Thread.Sleep(surplus);
                while (tmpDelay > 0)
                {
                    Thread.Sleep(1000);
                    tmpDelay -= 1000;
                    if (delay != Delay)
                    {
                        tmpDelay = Delay - (delay - tmpDelay);
                        delay = Delay;
                    }
                }
            }

            Thread.Sleep(Delay);
        }

        public string DisplayJournal()
        {
            var stringBuilder = new StringBuilder("На диск были записаны: " + Environment.NewLine);
            foreach (var item in _messageJournal.Where(x => x.IsRecorded))
            {
                stringBuilder.Append(item.Message + Environment.NewLine);
            }

            return stringBuilder.ToString();
        }

        public string DisplayQueue()
        {
            var stringBuilder = new StringBuilder("В очереди для записи на диск: " + Environment.NewLine);
            foreach (var item in _messageJournal.Where(x => !x.IsRecorded))
            {
                stringBuilder.Append(item.Message + Environment.NewLine);
            }

            return stringBuilder.ToString();
        }

        public string DisplayLog()
        {
            var stringBuilder = new StringBuilder("В очереди для записи на диск: " + Environment.NewLine);
            _messageJournal.ForEach(item => stringBuilder.Append(item.Message + Environment.NewLine));
            return stringBuilder.ToString();
        }
    }
}