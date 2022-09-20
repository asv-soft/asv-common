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
            this._input.SelectMany<byte[], byte>((Func<byte[], IEnumerable<byte>>)(_ => (IEnumerable<byte>)_)).Subscribe<byte>(new Action<byte>(this.OnData), this._cancel.Token);
            if (_input is IPort port)
            {
                port.State.Subscribe(_onPortState, _cancel.Token);
                _onPortState.OnNext(port.State.Value);
            }
            else _onPortState.OnNext(PortState.Connected);
        }

        private void OnData(byte data)
        {
            if (!this._sync)
            {
                if ((int)data != (int)this._config.StartByte)
                    return;
                this._sync = true;
                this._readIndex = 0;
            }
            else if ((int)data == (int)this._config.StopByte)
            {
                this._sync = false;
                try
                {
                    this._output.OnNext(this._config.DefaultEncoding.GetString(this._buffer, 0, this._readIndex));
                }
                catch (Exception ex)
                {
                    this._onErrorSubject.OnNext(ex);
                }
            }
            else
            {
                this._buffer[this._readIndex] = data;
                ++this._readIndex;
                if (this._readIndex >= this._config.MaxMessageSize)
                {
                    this._onErrorSubject.OnNext(new Exception(string.Format("Receive buffer overflow. Max message size={0}", (object)this._config.MaxMessageSize)));
                    this._sync = false;
                }
            }
        }

        public void Dispose()
        {
            this._cancel.Cancel(false);
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            return this._output.Subscribe(observer);
        }

        public IRxValue<PortState> OnPortState => _onPortState;

        public IObservable<Exception> OnError
        {
            get
            {
                return (IObservable<Exception>)this._onErrorSubject;
            }
        }

        public async Task Send(string value, CancellationToken cancel)
        {
            
            try
            {
                using var linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(cancel, this._cancel.Token);
                byte[] data = this._config.DefaultEncoding.GetBytes(_config.StartByte + value + _config.StopByte);
                await this._input.Send(data, data.Length, linkedCancel.Token).ConfigureAwait(false);
                data = (byte[])null;
            }
            catch (Exception ex)
            {
                this._onErrorSubject.OnNext(new Exception(string.Format("Error to send text stream data '{0}':{1}", (object)value, (object)ex.Message), ex));
                throw;
            }
           
        }
    }
}
