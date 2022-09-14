using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reactive.Subjects;

namespace Asv.Common
{




    public class ProcessRx : IDisposable
    {
        private Process _process;
        private readonly BlockingCollection<string> _output = new();
        private StreamWriter _input;
        private readonly Subject<string> _inputSubject = new();
        private readonly Subject<string> _outputSubject = new();
        private readonly Subject<string> _errorSubject = new();
        private const int DefaultTimeoutMs = 5000;

        public IObservable<string> OnInput => _inputSubject;
        public IObservable<string> OnOutput => _outputSubject;
        public IObservable<string> OnError => _errorSubject;

        public Process Process => _process;

        public void Start(string filePath, string args, bool createNoWindow = false)
        {
            _process = Process.Start(new ProcessStartInfo(filePath, args)
            {

                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = createNoWindow,
                UseShellExecute = false,
            });
            if (_process == null)
                throw new Exception(string.Format("Error to run '{0}'", filePath));

            _process.ErrorDataReceived += (sender, eventArgs) =>
            {
                if (!_errorSubject.IsDisposed)
                {
                    _errorSubject.OnNext(eventArgs.Data);
                }
            };

            _process.OutputDataReceived += (sender, eventArgs) =>
            {
                if (!_outputSubject.IsDisposed || _output.IsAddingCompleted == false)
                {
                    _outputSubject.OnNext(eventArgs.Data);
                    _output.Add(eventArgs.Data);
                }
            };

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            _process.EnableRaisingEvents = true;
            _input = _process.StandardInput;
        }


        public void ClearOutput()
        {
            string val;
            while (_output.TryTake(out val))
            {

            }
        }

        public IEnumerable<string> ReadToEnd()
        {
            string val;
            while (_output.TryTake(out val))
            {
                yield return val;
            }
        }

        public void Push(string value)
        {
            _input.WriteLine(value);
            _input.BaseStream.Flush();
            _inputSubject.OnNext(value);
        }

        public string Pop(int? timeoutMs = null)
        {
            timeoutMs = timeoutMs ?? DefaultTimeoutMs;
            string val;
            if (!_output.TryTake(out val, timeoutMs.Value))
                throw new Exception("Timeout to get output value");

            return val;
        }

        public virtual void Dispose()
        {
            _output.CompleteAdding();
            _inputSubject.Dispose();
            _outputSubject.Dispose();
            _errorSubject.Dispose();
            if (_process?.HasExited == false) _process?.Kill();
            _process?.Dispose();

        }


        public void WaitForExit()
        {
            _process.WaitForExit();
        }
    }

}
