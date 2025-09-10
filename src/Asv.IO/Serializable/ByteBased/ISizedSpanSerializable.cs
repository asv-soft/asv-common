using System;

namespace Asv.IO
{
    public interface ISizedSpanSerializable : ISpanSerializable
    {
        int GetByteSize();
    }

    public delegate T DeserializeDelegate<out T>(ref ReadOnlySpan<byte> data);
    public delegate void SerializeDelegate<in T>(ref Span<byte> data, T value);
    public delegate int SerializeSizeDelegate<in T>(T value);
}
