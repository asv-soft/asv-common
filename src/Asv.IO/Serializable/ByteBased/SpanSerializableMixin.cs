using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Asv.IO;

public static class SpanSerializableMixin
{
    public static int Serialize(
        this ISpanSerializable src,
        byte[] destination,
        int start = 0,
        int length = -1
    )
    {
        if (length < 0)
        {
            length = destination.Length - start;
        }
        var span = new Span<byte>(destination, start, length);
        src.Serialize(ref span);
        return length - span.Length;
    }

    public static ValueTask<int> Serialize(this ISpanSerializable src, Memory<byte> destination)
    {
        var span = destination.Span;
        src.Serialize(ref span);
        return ValueTask.FromResult(destination.Length - span.Length);
    }

    public static ValueTask<int> Deserialize(
        this ISpanSerializable src,
        ReadOnlyMemory<byte> destination
    )
    {
        var span = destination.Span;
        src.Deserialize(ref span);
        return ValueTask.FromResult(destination.Length - span.Length);
    }

    public static int Deserialize(
        this ISpanSerializable src,
        byte[] destination,
        int start = 0,
        int length = -1
    )
    {
        if (length < 0)
        {
            length = destination.Length - start;
        }
        var span = new ReadOnlySpan<byte>(destination, start, length);
        src.Deserialize(ref span);
        return length - span.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(this ISizedSpanSerializable value, IBufferWriter<byte> buffer) =>
        Serialize(value, buffer, value.GetByteSize());

    public static void Serialize(this ISpanSerializable value, IBufferWriter<byte> buffer, int size)
    {
        var writer = buffer.GetSpan(size);
        value.Serialize(ref writer);
        buffer.Advance(size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Deserialize(
        this ISizedSpanSerializable value,
        ref SequenceReader<byte> reader
    ) => Deserialize(value, ref reader, value.GetByteSize());

    public static bool Deserialize(
        this ISpanSerializable value,
        ref SequenceReader<byte> reader,
        int size
    )
    {
        if (reader.Remaining < size)
        {
            return false;
        }

        // If the data is in the current span, we can use it directly
        if (reader.CurrentSpan.Length - reader.CurrentSpanIndex >= size)
        {
            var span = reader.CurrentSpan.Slice(reader.CurrentSpanIndex, size);
            reader.Advance(size);
            value.Deserialize(ref span);
        }
        else
        {
            // Otherwise, we need to rent a buffer from the pool
            var rentedArray = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                var buffer = rentedArray.AsSpan(0, size);

                if (!reader.TryCopyTo(buffer))
                {
                    return false;
                }
                reader.Advance(size);
                var span = new ReadOnlySpan<byte>(rentedArray, 0, size);
                value.Deserialize(ref span);
                Debug.Assert(
                    rentedArray.Length == span.Length,
                    "Read not all data from rented array"
                );
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedArray);
            }
        }

        return true;
    }

    public static bool Deserialize(
        this ISizedSpanSerializable value,
        ref ReadOnlySequence<byte> rdr
    )
    {
        var reader = new SequenceReader<byte>(rdr);
        return Deserialize(value, ref reader);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(this ISizedSpanSerializable item, Stream dest) =>
        Serialize(item, dest, item.GetByteSize());

    /// <summary>
    /// Create new byte array and serialize item to it
    /// Do not use it in performance critical code
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Serialize(this ISizedSpanSerializable item)
    {
        var size = item.GetByteSize();
        var array = new byte[size];
        var wSize = item.Serialize(array);
        if (wSize != size)
        {
            throw new Exception(
                $"Error to serialize item {item}: file length error. Want write {size} bytes. Writed {wSize} bytes."
            );
        }

        return array;
    }

    public static void Serialize(this ISpanSerializable item, Stream dest, int maxSize)
    {
        var array = ArrayPool<byte>.Shared.Rent(maxSize);
        try
        {
            var span = new Span<byte>(array, 0, maxSize);
            item.Serialize(ref span);
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = 0;
            }

            dest.Write(array, 0, maxSize);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Deserialize(this ISizedSpanSerializable value, Stream src) =>
        Deserialize(value, src, value.GetByteSize());

    public static void Deserialize(this ISpanSerializable item, Stream src, int size)
    {
        var array = ArrayPool<byte>.Shared.Rent(size);

        try
        {
            var read = src.Read(array, 0, size);
            if (read != size)
                throw new Exception(
                    $"Error to read item {item}: file length error. Want read {size} bytes. Got {read} bytes."
                );
            var span = new ReadOnlySpan<byte>(array, 0, size);
            item.Deserialize(ref span);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }

    /// <summary>
    /// Copy data from src to dest by serializing and deserializing
    /// </summary>
    /// <param name="src">Source</param>
    /// <param name="dest">Destination</param>
    /// <returns></returns>
    public static void CopyTo<T>(this T src, T dest)
        where T : ISizedSpanSerializable
    {
        var size = src.GetByteSize();
        var array = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            var span = new Span<byte>(array, 0, size);
            src.Serialize(ref span);
            var readSpan = new ReadOnlySpan<byte>(array, 0, size);
            dest.Deserialize(ref readSpan);
            Debug.Assert(span.Length == readSpan.Length);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }

    /// <summary>
    /// Create new instance of T and copy data from src to new instance
    /// </summary>
    /// <param name="src"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T BinaryClone<T>(this T src)
        where T : ISizedSpanSerializable, new()
    {
        var dest = new T();
        var size = src.GetByteSize();
        var array = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            var span = new Span<byte>(array, 0, size);
            src.Serialize(ref span);
            var readSpan = new ReadOnlySpan<byte>(array, 0, size);
            dest.Deserialize(ref readSpan);
            Debug.Assert(span.Length == readSpan.Length);
            return dest;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }
}
