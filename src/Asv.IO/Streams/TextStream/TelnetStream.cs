using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;

namespace Asv.IO
{
    public class TelnetStream : ITextStream
    {
        private enum State
        {
            Start,
            Process
        }

        private readonly char[] _endBytes = {'\r', '\n'};
        
        private readonly CancellationTokenSource _cancel = new();
        private readonly TelnetConfig _config;
        private State _sync = State.Start;
        private int _readIndex = 0;
        private readonly Subject<string> _output = new();
        private readonly Subject<Exception> _onErrorSubject = new();
        private readonly byte[] _buffer;
        private readonly TcpClientPort _input;
        private readonly RxValue<PortState> _onPortState = new();
        

        public TelnetStream(TelnetConfig config)
        {
            _config = config ?? new TelnetConfig();
            _buffer = new byte[_config.MaxMessageSize];
            _input = ConnectionStringConvert(_config.ConnectionString);
            _input.State.Subscribe(_onPortState, _cancel.Token);
            _onPortState.OnNext(_input.State.Value);
            _input.SelectMany<byte[], byte>((Func<byte[], IEnumerable<byte>>)(_ => (IEnumerable<byte>)_)).Subscribe<byte>(new Action<byte>(OnData), _cancel.Token);
            _cancel.Token.Register((Action)(() =>
            {
                _onPortState.OnNext(PortState.Disabled);
                _onPortState.OnCompleted();
                _onPortState.Dispose();
                _output.Dispose();
                _onErrorSubject.OnCompleted();
                _onErrorSubject.Dispose();
                _input?.Dispose();
            }));
        }

        private static TcpClientPort ConnectionStringConvert(string connString)
        {
            var p = (TcpClientPort)PortFactory.Create(connString);
            p.Enable();
            return p;
        }

        private void OnData(byte data)
        {
            switch (_sync)
            {
                case State.Start:
                    _sync = (data != (byte)_endBytes[0] && data != (byte)_endBytes[1]) ? State.Process : State.Start;
                    _readIndex = 0;
                    if (_sync == State.Process)
                    {
                        _buffer[_readIndex] = data;
                        ++_readIndex;
                    }
                    break;
                case State.Process:
                    if (data == (byte)_endBytes[0] || data == (byte)_endBytes[1])
                    {
                        _sync = State.Start;
                        try
                        {
                            _output.OnNext(_config.DefaultEncoding.GetString(_buffer, 0, _readIndex));
                        }
                        catch (Exception ex)
                        {
                            _onErrorSubject.OnNext(ex);
                        }
                    }
                    else
                    {
                        _buffer[_readIndex] = data;
                        ++_readIndex;
                        if (_readIndex >= _config.MaxMessageSize)
                        {
                            _onErrorSubject.OnNext(new Exception(string.Format("Receive buffer overflow. Max message size={0}", (object)_config.MaxMessageSize)));
                            _sync = State.Start;
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Dispose()
        {
            _cancel.Cancel(false);
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            return _output.Subscribe(observer);
        }

        public IRxValue<PortState> OnPortState => _onPortState;
        public IObservable<Exception> OnError => (IObservable<Exception>)_onErrorSubject;

        public async Task Send(string value, CancellationToken cancel)
        {
            
            try
            {
                using var  linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(cancel, _cancel.Token);
                var data = _config.DefaultEncoding.GetBytes(value + _endBytes[0] + _endBytes[1]);
                await _input.Send(data, data.Length, linkedCancel.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _onErrorSubject.OnNext(new Exception(string.Format("Error to send text stream data '{0}':{1}", (object)value, (object)ex.Message), ex));
                throw;
            }
        }

        public async Task<string> RequestText(string request, int timeoutMs, CancellationToken cancel)
        {

            using var linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(cancel);
                linkedCancel.CancelAfter(timeoutMs);
                var tcs = new TaskCompletionSource<string>();
                using var c1 = linkedCancel.Token.Register(tcs.SetCanceled);
                try
                {
                    using var subscribe = this.FirstAsync().Subscribe(tcs.SetResult);
                    try
                    {
                        var data = _config.DefaultEncoding.GetBytes(request + _endBytes[0] + _endBytes[1]);
                        await _input.Send(data, data.Length, linkedCancel.Token).ConfigureAwait(false);
                        return await tcs.Task.ConfigureAwait(false);
                    }
                    finally
                    {
                        subscribe?.Dispose();
                    }
                }
                finally
                {
                    c1.Dispose();
                }
        }
    }
}
