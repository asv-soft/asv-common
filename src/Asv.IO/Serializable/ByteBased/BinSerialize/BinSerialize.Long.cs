using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace Asv.IO;

public partial class BinSerialize
{
    /// <summary>
    /// 'Reserve' space for a signed 64 bit integer.
    /// </summary>
    /// <remarks>
    /// Will consume 8 bytes.
    /// </remarks>
    /// <param name="span">Span to reserver from.</param>
    /// <returns>Reference to the reserved space.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref long ReserveLong(ref Span<byte> span)
    {
        ref var result = ref Unsafe.As<byte, long>(ref span[0]);

        // Init to default, as otherwise it would be whatever data was at that memory.
        result = default;

        // 'Advance' the span.
        span = span[sizeof(long)..];
        return ref result;
    }

    #region WriteLong

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteLong(Stream stream, in long val)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64LittleEndian(buffer, val);
        stream.Write(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteLong(Stream stream, long val)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64LittleEndian(buffer, val);
        stream.Write(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteLong(ref Span<byte> span, long val)
    {
        BinaryPrimitives.WriteInt64LittleEndian(span, val);

        // 'Advance' the span.
        span = span[sizeof(long)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteLong(ref Span<byte> span, in long val)
    {
        BinaryPrimitives.WriteInt64LittleEndian(span, val);

        // 'Advance' the span.
        span = span[sizeof(long)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteLong(Span<byte> span, in long val)
    {
        BinaryPrimitives.WriteInt64LittleEndian(span, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteLong(ref Memory<byte> memory, in long val)
    {
        BinaryPrimitives.WriteInt64LittleEndian(memory.Span, val);

        // 'Advance' the memory.
        memory = memory[sizeof(long)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteLong(Memory<byte> memory, in long val)
    {
        BinaryPrimitives.WriteInt64LittleEndian(memory.Span, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteLong(IBufferWriter<byte> wrt, in long val)
    {
        var span = wrt.GetSpan(sizeof(long));
        WriteLong(ref span, in val);
        wrt.Advance(sizeof(long));
    }

    #endregion

    #region ReadLong

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadLong(Stream stream, ref long value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        stream.ReadExactly(buffer);
        value = BinaryPrimitives.ReadInt64LittleEndian(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ReadLong(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadInt64LittleEndian(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ReadLong(ref ReadOnlySpan<byte> span)
    {
        var result = BinaryPrimitives.ReadInt64LittleEndian(span);

        // 'Advance' the span.
        span = span[sizeof(long)..];

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadLong(ref ReadOnlySpan<byte> span, ref long value)
    {
        value = BinaryPrimitives.ReadInt64LittleEndian(span);

        // 'Advance' the span.
        span = span[sizeof(long)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadLong(ReadOnlySpan<byte> span, ref long value)
    {
        value = BinaryPrimitives.ReadInt64LittleEndian(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadLong(ref ReadOnlyMemory<byte> memory, ref long value)
    {
        value = BinaryPrimitives.ReadInt64LittleEndian(memory.Span);

        // 'Advance' the span.
        memory = memory[sizeof(long)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadLong(ReadOnlyMemory<byte> memory, ref long value)
    {
        value = BinaryPrimitives.ReadInt64LittleEndian(memory.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadLong(ref SequenceReader<byte> reader, ref long value)
    {
        const int size = sizeof(long);

        // Not enough data available.
        if (reader.Remaining < size)
        {
            return false;
        }

        // Fast path: all required bytes are in the current unread span.
        if (reader.UnreadSpan.Length >= size)
        {
            var ro = reader.UnreadSpan.Slice(0, size);
            ReadLong(ref ro, ref value);
            reader.Advance(size);
            return true;
        }

        // Fallback: data spans multiple segments, copy to a stack buffer.
        Span<byte> buf = stackalloc byte[size];
        if (!reader.TryCopyTo(buf))
        {
            return false; // Safety net, though Remaining check should prevent this
        }

        reader.Advance(size);
        ReadOnlySpan<byte> tmp = buf;
        ReadLong(ref tmp, ref value);
        return true;
    }

    #endregion
}
