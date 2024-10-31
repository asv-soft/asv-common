using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;

namespace Asv.IO
{
    public class TextStreamBase : ITextStream, IDisposable, IObservable<string>
    {
        private readonly CancellationTokenSource _cancel = new();
        private bool _sync = false;
        private int _readIndex = 0;
        private readonly Subject<string> _output = new();
        private readonly RxValue<PortState> _onPortState = new();
        private readonly Subject<Exception> _onErrorSubject = new();
        private readonly byte[] _buffer;
        private readonly TextReaderBaseConfig _config;
        private readonly IDataStream _input;

        public TextStreamBase(IDataStream strm, TextReaderBaseConfig config = null)
        {
            this._cancel.Token.Register((Action)(() => this._output.Dispose()));
            this._config = config ?? new TextReaderBaseConfig();
            this._buffer = new byte[this._config.MaxMessageSize];
            this._input = strm;
            this._input.SelectMany<byte[], byte>(
                    (Func<byte[], IEnumerable<byte>>)(_ => (IEnumerable<byte>)_)
                )
                .Subscribe<byte>(new Action<byte>(this.OnData), this._cancel.Token);
            if (_input is IPort port)
            {
                port.State.Subscribe(_onPortState, _cancel.Token);
                _onPortState.OnNext(port.State.Value);
            }
            else
            {
                _onPortState.OnNext(PortState.Connected);
            }
        }

        private void OnData(byte data)
        {
            if (!_sync)
            {
                if (data != _config.StartByte)
                {
                    return;
                }

                _sync = true;
                _readIndex = 0;
            }
            else if (data == _config.StopByte)
            {
                _sync = false;
                try
                {
                    _output.OnNext(_config.DefaultEncoding.GetString(_buffer, 0, this._readIndex));
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
                    _onErrorSubject.OnNext(
                        new Exception(
                            string.Format(
                                "Receive buffer overflow. Max message size={0}",
                                _config.MaxMessageSize
                            )
                        )
                    );
                    _sync = false;
                }
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

        public IObservable<Exception> OnError => _onErrorSubject;

        public async Task Send(string value, CancellationToken cancel)
        {
            try
            {
                using var linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(
                    cancel,
                    _cancel.Token
                );
                byte[] data = _config.DefaultEncoding.GetBytes(
                    _config.StartByte + value + _config.StopByte
                );
                await _input.Send(data, data.Length, linkedCancel.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _onErrorSubject.OnNext(
                    new Exception(
                        string.Format(
                            "Error to send text stream data '{0}':{1}",
                            value,
                            ex.Message
                        ),
                        ex
                    )
                );
                throw;
            }
        }
    }
}
