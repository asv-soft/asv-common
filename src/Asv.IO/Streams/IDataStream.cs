using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Asv.IO
{
    public interface IDataStream
    {
        Observable<byte[]> OnReceive { get; }
        string Name { get; }
        Task<bool> Send(byte[] data, int count, CancellationToken cancel);
        Task<bool> Send(ReadOnlyMemory<byte> data, CancellationToken cancel);
        long RxBytes { get; }
        long TxBytes { get; }
    }

    public static class DataStreamHelper
    {
        public static Task<bool> Send(
            this IDataStream src,
            ISizedSpanSerializable data,
            CancellationToken cancel = default
        )
        {
            return Send(src, data, out _, cancel);
        }

        public static Task<bool> Send(
            this IDataStream src,
            ISizedSpanSerializable data,
            out int byteSent,
            CancellationToken cancel = default
        )
        {
            return Send(src, data, out byteSent, ArrayPool<byte>.Shared, cancel);
        }

        /// <summary>
        /// Encodes a message once and sends only the encoded bytes.
        /// </summary>
        /// <remarks>
        /// The caller owns the encoder. These helpers do not synchronize it; a shared stateful
        /// encoder requires serialization of the entire encode and send operation. A send failure
        /// can leave the output partially written and requires protocol-specific recovery.
        /// </remarks>
        public static Task<bool> Send(
            this IDataStream src,
            IProtocolMessage data,
            IProtocolEncoder encoder,
            CancellationToken cancel
        )
        {
            return Send(src, data, encoder, ArrayPool<byte>.Shared, cancel);
        }

        internal static Task<bool> Send(
            IDataStream src,
            ISizedSpanSerializable data,
            out int byteSent,
            ArrayPool<byte> pool,
            CancellationToken cancel = default
        )
        {
            var size = data.GetByteSize();
            var array = pool.Rent(size);
            var span = new Span<byte>(array, 0, size);
            try
            {
                byteSent = span.Length;
                data.Serialize(ref span);
                byteSent -= span.Length;
                if ((uint)byteSent > (uint)size)
                {
                    throw new InvalidOperationException(
                        "The serializer returned an invalid remaining span length."
                    );
                }

                return SendAndReturn(src.Send(array, byteSent, cancel), array, pool);
            }
            catch
            {
                pool.Return(array);
                throw;
            }
        }

        internal static Task<bool> Send(
            IDataStream src,
            IProtocolMessage data,
            IProtocolEncoder encoder,
            ArrayPool<byte> pool,
            CancellationToken cancel = default
        )
        {
            ArgumentNullException.ThrowIfNull(src);
            cancel.ThrowIfCancellationRequested();
            var size = ProtocolEncodingMixin.GetMaxByteSize(encoder, data);
            var array = pool.Rent(size);
            try
            {
                var written = ProtocolEncodingMixin.EncodeChecked(
                    encoder,
                    data,
                    array.AsSpan(0, size)
                );
                return SendAndReturn(src.Send(array, written, cancel), array, pool);
            }
            catch
            {
                pool.Return(array);
                throw;
            }
        }

        private static async Task<bool> SendAndReturn(
            Task<bool> pendingSend,
            byte[] buffer,
            ArrayPool<byte> pool
        )
        {
            try
            {
                return await pendingSend.ConfigureAwait(false);
            }
            finally
            {
                pool.Return(buffer);
            }
        }
    }
}
