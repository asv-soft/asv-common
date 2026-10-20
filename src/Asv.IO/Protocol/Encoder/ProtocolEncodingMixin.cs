using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

/// <summary>
/// Encodes wire data using an explicitly owned encoder and the actual encoded length.
/// </summary>
/// <remarks>
/// These methods do not synchronize access to the encoder. When an encoder maintains stream
/// state, its owner must serialize the entire encode and write operation for that stream.
/// A write failure can leave a stream partially written; do not retry with the same encoder
/// unless the protocol defines how to recover.
/// </remarks>
public static class ProtocolEncodingMixin
{
    /// <summary>
    /// Encodes the message once and returns only the bytes written.
    /// </summary>
    public static byte[] EncodeToArray(this IProtocolMessage message, IProtocolEncoder encoder)
    {
        var size = GetMaxByteSize(encoder, message);
        var buffer = new byte[size];
        var written = EncodeChecked(encoder, message, buffer);
        if (written != buffer.Length)
        {
            Array.Resize(ref buffer, written);
        }

        return buffer;
    }

    /// <summary>
    /// Encodes the message once and advances the writer by the actual number of bytes written.
    /// </summary>
    public static int EncodeTo(
        this IProtocolMessage message,
        IBufferWriter<byte> destination,
        IProtocolEncoder encoder
    )
    {
        ArgumentNullException.ThrowIfNull(destination);
        var size = GetMaxByteSize(encoder, message);
        var written = EncodeChecked(encoder, message, destination.GetSpan(size)[..size]);
        destination.Advance(written);
        return written;
    }

    /// <summary>
    /// Encodes the message once and writes only the encoded bytes to the stream.
    /// </summary>
    public static int EncodeToStream(
        this IProtocolMessage message,
        Stream destination,
        IProtocolEncoder encoder
    )
    {
        ArgumentNullException.ThrowIfNull(destination);
        var size = GetMaxByteSize(encoder, message);
        var buffer = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            var written = EncodeChecked(encoder, message, buffer.AsSpan(0, size));
            destination.Write(buffer, 0, written);
            return written;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Encodes the message once and retains the encoded buffer until the asynchronous write completes.
    /// </summary>
    public static async ValueTask<int> EncodeToStreamAsync(
        this IProtocolMessage message,
        Stream destination,
        IProtocolEncoder encoder,
        CancellationToken cancel = default
    )
    {
        ArgumentNullException.ThrowIfNull(destination);
        cancel.ThrowIfCancellationRequested();
        var size = GetMaxByteSize(encoder, message);
        var buffer = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            var written = EncodeChecked(encoder, message, buffer.AsSpan(0, size));
            await destination.WriteAsync(buffer.AsMemory(0, written), cancel).ConfigureAwait(false);
            return written;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    internal static int GetMaxByteSize(IProtocolEncoder encoder, IProtocolMessage message)
    {
        ArgumentNullException.ThrowIfNull(encoder);
        ArgumentNullException.ThrowIfNull(message);
        var size = encoder.GetMaxByteSize(message);
        if (size < 0)
        {
            throw new InvalidOperationException(
                "The encoder returned a negative maximum byte size."
            );
        }

        return size;
    }

    internal static int EncodeChecked(
        IProtocolEncoder encoder,
        IProtocolMessage message,
        Span<byte> destination
    )
    {
        var written = encoder.Encode(message, destination);
        if ((uint)written > (uint)destination.Length)
        {
            throw new InvalidOperationException(
                $"The encoder returned {written} bytes for a {destination.Length}-byte destination."
            );
        }

        return written;
    }
}
