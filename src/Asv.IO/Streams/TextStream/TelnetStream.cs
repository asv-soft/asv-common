using System;
using System.Buffers;
using System.IO;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;

namespace Asv.IO
{
    public class TelnetStream : DisposableOnceWithCancel,ITextStream
    {
        private readonly byte[] _endBytes;
        private readonly IDataStream _input;
        private readonly Encoding _encoding;
        private int _readIndex = 0;
        private readonly Subject<string> _output = new();
        private readonly Subject<Exception> _onErrorSubject;
        private readonly byte[] _buffer;
        private readonly object _sync = new();


        public TelnetStream(IDataStream strm, Encoding encoding, int bufferSize = 10*1024, string endChars = "\r\n")
        {
            _input = strm ?? throw new ArgumentNullException(nameof(strm));
            _encoding = encoding;
            _buffer = new byte[bufferSize];
            _input.Subscribe(OnData).DisposeItWith(Disposable);
            _onErrorSubject = new Subject<Exception>().DisposeItWith(Disposable);
            _endBytes = _encoding.GetBytes(endChars);
        }


        private void OnData(byte[] dataArray)
        {
            lock (_sync)
            {
                foreach (var data in dataArray)
                {
                    if (_readIndex >= _buffer.Length)
                    {
                        _onErrorSubject.OnNext(new InternalBufferOverflowException(string.Format("Receive buffer overflow. Max message size={0}", _buffer.Length)));
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

       

        public IDisposable Subscribe(IObserver<string> observer)
        {
            return _output.Subscribe(observer);
        }

        public IObservable<Exception> OnError => _onErrorSubject;

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
                    string.Format("Error to send text stream data '{0}':{1}", (object)value, (object)ex.Message), ex));
                throw;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(data);
            }
        }

        
    }
}
