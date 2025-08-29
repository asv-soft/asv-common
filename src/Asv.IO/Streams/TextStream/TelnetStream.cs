using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Asv.IO
{
    public class TelnetStream : ITextStream
    {
        private readonly byte[] _endBytes;
        private readonly IDataStream _input;
        private readonly Encoding _encoding;
        private int _readIndex = 0;
        private readonly Subject<string> _output = new();
        private readonly Subject<Exception> _onErrorSubject;
        private readonly byte[] _buffer;
        private readonly object _sync = new();
        private readonly Subject<string> _onReceive = new();
        private readonly IDisposable _sub1;
        private readonly CancellationTokenSource _disposeCancel = new();


        public TelnetStream(IDataStream strm, Encoding encoding, int bufferSize = 10*1024, string endChars = "\r\n")
        {
            _input = strm ?? throw new ArgumentNullException(nameof(strm));
            _encoding = encoding;
            _buffer = new byte[bufferSize];
            _sub1 = _input.OnReceive.Subscribe(OnData);
            _onErrorSubject = new Subject<Exception>();
            _endBytes = _encoding.GetBytes(endChars);
        }


        private void OnData(byte[] dataArray)
        {
            lock(_sync)
            {
                foreach (var data in dataArray)
                {
                    if (_readIndex >= _buffer.Length)
                    {
                        _onErrorSubject.OnNext(new InternalBufferOverflowException(
                            $"Receive buffer overflow. Max message size={_buffer.Length}"));
                        _readIndex = 0;
                    }
                    _buffer[_readIndex++] = data;
                    if (_readIndex <= _endBytes.Length) continue;
                    // trying to find message end
                    var findEnd = true;
                    var startIndex = _readIndex - _endBytes.Length;
                    for (var i = 0; i < _endBytes.Length; i++)
                    {
                        if (_buffer[startIndex+i] != _endBytes[i])
                        {
                            findEnd = false;
                            break;
                        }
                    }
                    if (!findEnd) continue;
                    try
                    {
                        _output.OnNext(_encoding.GetString(_buffer, 0, _readIndex - _endBytes.Length));
                    }
                    catch (Exception ex)
                    {
                        _onErrorSubject.OnNext(ex);
                    }
                    finally
                    {
                        _readIndex = 0;
                    }
                    
                }
            }
        }
        private CancellationToken DisposeCancel => _disposeCancel.Token;
        public Observable<string> OnReceive => _onReceive;

        public Observable<Exception> OnError => _onErrorSubject;

        public async Task Send(string value, CancellationToken cancel)
        {
            var dataSize = _encoding.GetByteCount(value) + _endBytes.Length;
            var data = ArrayPool<byte>.Shared.Rent(dataSize);
            try
            {
                using var linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(cancel, DisposeCancel);
                
                var writed = _encoding.GetBytes(value,0,value.Length, data,0);
                _endBytes.CopyTo(data, writed);
                await _input.Send(data, dataSize, linkedCancel.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _onErrorSubject.OnNext(new Exception(
                    $"Error to send text stream data '{value}':{ex.Message}", ex));
                throw;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(data);
            }
        }


        #region Dispose

        public void Dispose()
        {
            _output.Dispose();
            _onErrorSubject.Dispose();
            _onReceive.Dispose();
            _sub1.Dispose();
            _disposeCancel.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await CastAndDispose(_output);
            await CastAndDispose(_onErrorSubject);
            await CastAndDispose(_onReceive);
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

        #endregion
    }
}
