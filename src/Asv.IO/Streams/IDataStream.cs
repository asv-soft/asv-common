using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO
{
    public interface IDataStream:IObservable<byte[]>
    {
        string Name { get; }
        Task<bool> Send(byte[] data, int count, CancellationToken cancel);
        long RxBytes { get; }
        long TxBytes { get; }
    }

    public static class DataStreamHelper
    {
        public static Task<bool> Send(this IDataStream src, ISizedSpanSerializable data,  CancellationToken cancel = default)
        {
            return Send(src, data, out var byteSent, cancel);
        }

        public static Task<bool> Send(this IDataStream src, ISizedSpanSerializable data,out int byteSent, CancellationToken cancel = default)
        {
            var size = data.GetByteSize();
            var array = ArrayPool<byte>.Shared.Rent(size);
            var span = new Span<byte>(array,0,size);
            try
            {
                byteSent = span.Length;
                data.Serialize(ref span);
                byteSent -= span.Length;
                return src.Send(array, size, cancel);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array);
            }
        }
    }

}
