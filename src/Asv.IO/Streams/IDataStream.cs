using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO
{
    public interface IDataStream : IObservable<byte[]>
    {
        string Name { get; }
        Task<bool> Send(byte[] data, int count, CancellationToken cancel);
        Task<bool> Send(ReadOnlyMemory<byte> data, CancellationToken cancel);
        long RxBytes { get; }
        long TxBytes { get; }
        public Task<bool> Send(ISizedSpanSerializable data, CancellationToken cancel = default)
        {
            return Send(data, out _, cancel);
        }

        public Task<bool> Send(ISizedSpanSerializable data, out int byteSent, CancellationToken cancel = default)
        {
            var size = data.GetByteSize();
            var array = ArrayPool<byte>.Shared.Rent(size);
            var span = new Span<byte>(array, 0, size);
            try
            {
                byteSent = span.Length;
                data.Serialize(ref span);
                byteSent -= span.Length;
                return Send(array, size, cancel);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array);
            }
        }
    }
}
