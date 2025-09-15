using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Asv.IO;

public partial class BinSerialize
{
    /// <summary>
    /// 'Reserve' space for a unsigned 64 bit integer.
    /// </summary>
    /// <remarks>
    /// Will consume 8 bytes.
    /// </remarks>
    /// <param name="span">Span to reserver from.</param>
    /// <returns>Reference to the reserved space.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref ulong ReserveULong(ref Span<byte> span)
    {
        ref var result = ref Unsafe.As<byte, ulong>(ref span[0]);

        // Init to default, as otherwise it would be whatever data was at that memory.
        result = default;

        // 'Advance' the span.
        span = span[sizeof(ulong)..];
        return ref result;
    }

    #region WriteULong

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteULong(ref Span<byte> span, ulong val)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(span, val);

        // 'Advance' the span.
        span = span[sizeof(ulong)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteULong(ref Span<byte> span, in ulong val)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(span, val);

        // 'Advance' the span.
        span = span[sizeof(ulong)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteULong(Span<byte> span, in ulong val)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(span, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteULong(ref Memory<byte> memory, in ulong val)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(memory.Span, val);

        // 'Advance' the span.
        memory = memory[sizeof(ulong)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteULong(Memory<byte> memory, in ulong val)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(memory.Span, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteULong(IBufferWriter<byte> wrt, in ulong val)
    {
        var span = wrt.GetSpan(sizeof(ulong));
        WriteULong(ref span, in val);
        wrt.Advance(sizeof(ulong));
    }

    #endregion

    #region ReadULong

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReadULong(ref ReadOnlySpan<byte> span)
    {
        var result = BinaryPrimitives.ReadUInt64LittleEndian(span);

        // 'Advance' the span.
        span = span[sizeof(ulong)..];

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadULong(ref ReadOnlySpan<byte> span, ref ulong value)
    {
        value = BinaryPrimitives.ReadUInt64LittleEndian(span);

        // 'Advance' the span.
        span = span[sizeof(ulong)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadULong(ReadOnlySpan<byte> span, ref ulong value)
    {
        value = BinaryPrimitives.ReadUInt64LittleEndian(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadULong(ref ReadOnlyMemory<byte> memory, ref ulong value)
    {
        value = BinaryPrimitives.ReadUInt64LittleEndian(memory.Span);

        // 'Advance' the span.
        memory = memory[sizeof(ulong)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadULong(ReadOnlyMemory<byte> memory, ref ulong value)
    {
        value = BinaryPrimitives.ReadUInt64LittleEndian(memory.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadULong(ref SequenceReader<byte> reader, ref ulong value)
    {
        const int size = sizeof(ulong);

        // Not enough data; do not advance the reader.
        if (reader.Remaining < size)
        {
            return false;
        }

        // Fast path: all bytes are in the current unread span.
        if (reader.UnreadSpan.Length >= size)
        {
            var ro = reader.UnreadSpan.Slice(0, size);
            ReadULong(ref ro, ref value);
            reader.Advance(size);
            return true;
        }

        // Fallback: data crosses segment boundary — copy to a stack buffer.
        Span<byte> buf = stackalloc byte[size];
        if (!reader.TryCopyTo(buf))
        {
            return false; // Safety net; Remaining check should make this unreachable.
        }

        reader.Advance(size);
        ReadOnlySpan<byte> tmp = buf;
        ReadULong(ref tmp, ref value);
        return true;
    }

    #endregion
}
