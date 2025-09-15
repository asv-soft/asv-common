using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Asv.IO;

public partial class BinSerialize
{
    /// <summary>
    /// 'Reserve' space for a signed 16 bit integer.
    /// </summary>
    /// <remarks>
    /// Will consume 2 bytes.
    /// </remarks>
    /// <param name="span">Span to reserver from.</param>
    /// <returns>Reference to the reserved space.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref short ReserveShort(ref Span<byte> span)
    {
        ref var result = ref Unsafe.As<byte, short>(ref span[0]);

        // Init to default, as otherwise it would be whatever data was at that memory.
        result = default;

        // 'Advance' the span.
        span = span[sizeof(short)..];
        return ref result;
    }

    #region WriteShort

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteShort(ref Span<byte> span, short val)
    {
        BinaryPrimitives.WriteInt16LittleEndian(span, val);

        // 'Advance' the span.
        span = span[sizeof(short)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteShort(Span<byte> span, short val)
    {
        BinaryPrimitives.WriteInt16LittleEndian(span, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteShort(ref Memory<byte> span, short val)
    {
        BinaryPrimitives.WriteInt16LittleEndian(span.Span, val);

        // 'Advance' the span.
        span = span[sizeof(short)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteShort(Memory<byte> span, short val)
    {
        BinaryPrimitives.WriteInt16LittleEndian(span.Span, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteShort(IBufferWriter<byte> wrt, short val)
    {
        var span = wrt.GetSpan(sizeof(short));
        WriteShort(ref span, val);
        wrt.Advance(sizeof(short));
    }

    #endregion

    #region ReadShort

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ReadShort(ref ReadOnlySpan<byte> span)
    {
        var result = BinaryPrimitives.ReadInt16LittleEndian(span);

        // 'Advance' the span.
        span = span[sizeof(short)..];
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadShort(ref ReadOnlySpan<byte> span, ref short value)
    {
        value = BinaryPrimitives.ReadInt16LittleEndian(span);

        // 'Advance' the span.
        span = span[sizeof(short)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadShort(ReadOnlySpan<byte> span, ref short value)
    {
        value = BinaryPrimitives.ReadInt16LittleEndian(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ReadShort(ref ReadOnlyMemory<byte> memory)
    {
        var result = BinaryPrimitives.ReadInt16LittleEndian(memory.Span);

        // 'Advance' the span.
        memory = memory[sizeof(short)..];
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadShort(ref ReadOnlyMemory<byte> memory, ref short value)
    {
        value = BinaryPrimitives.ReadInt16LittleEndian(memory.Span);

        // 'Advance' the span.
        memory = memory[sizeof(short)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadShort(ReadOnlyMemory<byte> memory, ref short value)
    {
        value = BinaryPrimitives.ReadInt16LittleEndian(memory.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadShort(ref SequenceReader<byte> reader, ref short value)
    {
        const int size = sizeof(short);

        // Not enough data available; do not advance the reader.
        if (reader.Remaining < size)
        {
            return false;
        }

        // Fast path: required bytes are in the current unread span.
        if (reader.UnreadSpan.Length >= size)
        {
            var ro = reader.UnreadSpan.Slice(0, size);
            ReadShort(ref ro, ref value);
            reader.Advance(size);
            return true;
        }

        // Fallback: data crosses segment boundaries — copy into a stack buffer.
        Span<byte> buf = stackalloc byte[size];
        if (!reader.TryCopyTo(buf))
        {
            return false; // Safety net; Remaining check should guarantee success.
        }

        reader.Advance(size);
        ReadOnlySpan<byte> tmp = buf;
        ReadShort(ref tmp, ref value);
        return true;
    }

    #endregion
}
