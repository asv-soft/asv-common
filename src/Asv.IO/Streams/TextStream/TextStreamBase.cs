using System;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Asv.IO
{
    public class TextStreamBase : ITextStream, IDisposable, IAsyncDisposable
    {
        private bool _sync = false;
        private int _readIndex = 0;
        private readonly Subject<string> _output = new();
        private readonly Subject<Exception> _onErrorSubject = new();
        private readonly byte[] _buffer;
        private readonly TextReaderBaseConfig _config;
        private readonly IDataStream _input;
        private readonly IDisposable _sub1;
        private readonly CancellationTokenSource _disposeCancel = new();

        public TextStreamBase(IDataStream strm, TextReaderBaseConfig? config = null)
        {
            _config = config ?? new TextReaderBaseConfig();
            _buffer = new byte[_config.MaxMessageSize];
            _input = strm;
            _sub1 = _input.OnReceive.Subscribe(OnData);
        }
        
        

        private void OnData(byte[] arr)
        {
            foreach (var data in arr)
            {
                if (!_sync)
                {
                    if (data != _config.StartByte)
                        return;
                    _sync = true;
                    _readIndex = 0;
                }
                else if (data == _config.StopByte)
                {
                    _sync = false;
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
                        _onErrorSubject.OnNext(new Exception($"Receive buffer overflow. Max message size={_config.MaxMessageSize}"));
                        _sync = false;
                    }
                }
            }
        }
        public Observable<string> OnReceive => _output;
        public Observable<Exception> OnError => _onErrorSubject;
        public async Task Send(string value, CancellationToken cancel)
        {
            try
            {
                using var linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(cancel, DisposeCancel);
                byte[] data = _config.DefaultEncoding.GetBytes(_config.StartByte + value + _config.StopByte);
                await _input.Send(data, data.Length, linkedCancel.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _onErrorSubject.OnNext(new Exception($"Error to send text stream data '{value}':{ex.Message}", ex));
                throw;
            }
           
        }

        #region Dispose

        private CancellationToken DisposeCancel => _disposeCancel.Token;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _output.Dispose();
                _onErrorSubject.Dispose();
                _sub1.Dispose();
                _disposeCancel.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            await CastAndDispose(_output);
            await CastAndDispose(_onErrorSubject);
            await CastAndDispose(_sub1);
            await CastAndDispose(_disposeCancel);

            return;

            static async ValueTask CastAndDispose(IDisposable resource)
            {
                if (resource is IAsyncDisposable resourceAsyncDisposable)
                    await resourceAsyncDisposable.DisposeAsync();
                else
                    resource.Dispose();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
