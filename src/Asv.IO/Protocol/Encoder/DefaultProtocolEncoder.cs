using System;

namespace Asv.IO;

/// <summary>
/// Adapts existing message serializers without adding stream state or changing their wire format.
/// </summary>
public sealed class DefaultProtocolEncoder : IProtocolEncoder
{
    public static DefaultProtocolEncoder Instance { get; } = new();

    public int GetMaxByteSize(IProtocolMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var size = message.GetByteSize();
        if (size < 0)
        {
            throw new InvalidOperationException("A message size cannot be negative.");
        }

        return size;
    }

    public int Encode(IProtocolMessage message, Span<byte> destination)
    {
        var size = GetMaxByteSize(message);
        if (destination.Length < size)
        {
            throw new ArgumentException("The destination is too small.", nameof(destination));
        }

        var remaining = destination[..size];
        message.Serialize(ref remaining);
        var written = size - remaining.Length;
        if ((uint)written > (uint)size)
        {
            throw new InvalidOperationException(
                "The serializer returned an invalid remaining span."
            );
        }

        return written;
    }
}
