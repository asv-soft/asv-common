using System;
using System.Buffers;
using System.IO;

namespace Asv.IO
{
    public interface ISizedSpanSerializable: ISpanSerializable
    {
        int GetByteSize();
    }


    public delegate T DeserializeDelegate<out T>(ref ReadOnlySpan<byte> data);
    public delegate void SerializeDelegate<in T>(ref Span<byte> data, T value);
    public delegate int SerializeSizeDelegate<in T>(T value);

    public static class SpanSerializableHelper
    {
        public static T Deserialize<T>(ref ReadOnlySpan<byte> data) where T : ISizedSpanSerializable, new()
        {
            var result = new T();
            result.Deserialize(ref data);
            return result;
        }
        public static void Serialize<T>(ref Span<byte> data, T value) where T : ISizedSpanSerializable, new()
        {
            value.Serialize(ref data);
        }

        public static int SerializeSize<T>(T value) where T : ISizedSpanSerializable, new()
        {
            return value.GetByteSize();
        }

        

        public static void WriteToStream(this ISpanSerializable item, Stream file, int itemMaxSize)
        {
            var array = ArrayPool<byte>.Shared.Rent(itemMaxSize);
            try
            {
                var span = new Span<byte>(array, 0, itemMaxSize);
                item.Serialize(ref span);
                for (var i = 0; i < span.Length; i++) span[i] = 0;
                file.Write(array, 0, itemMaxSize);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array);
            }
        }

        public static void ReadFromStream(this ISpanSerializable item, Stream file, int offset)
        {
            var array = ArrayPool<byte>.Shared.Rent(offset);

            try
            {
                var readed = file.Read(array, 0, offset);
                if (readed != offset)
                    throw new Exception(
                        $"Error to read item {item}: file length error. Want read {offset} bytes. Got {readed} bytes.");
                var span = new ReadOnlySpan<byte>(array, 0, offset);
                item.Deserialize(ref span);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array);
            }
        }

    }
}
