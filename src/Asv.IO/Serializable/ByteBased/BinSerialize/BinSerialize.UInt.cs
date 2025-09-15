using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace Asv.IO;

public partial class BinSerialize
{
    /// <summary>
    /// 'Reserve' space for a unsigned 32 bit integer.
    /// </summary>
    /// <remarks>
    /// Will consume 4 bytes.
    /// </remarks>
    /// <param name="span">Span to reserver from.</param>
    /// <returns>Reference to the reserved space.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref uint ReserveUInt(ref Span<byte> span)
    {
        ref var result = ref Unsafe.As<byte, uint>(ref span[0]);

        // Init to default, as otherwise it would be whatever data was at that memory.
        result = default;

        // 'Advance' the span.
        span = span[sizeof(uint)..];
        return ref result;
    }

    #region WriteUInt

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt(Stream stream, in uint val)
    {
        Span<byte> span = stackalloc byte[sizeof(uint)];
        WriteUInt(span, val);
        stream.Write(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt(ref Span<byte> span, uint val)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(span, val);

        // 'Advance' the span.
        span = span[sizeof(uint)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt(Span<byte> span, uint val)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(span, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt(ref Memory<byte> memory, uint val)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(memory.Span, val);

        // 'Advance' the span.
        memory = memory[sizeof(uint)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt(Memory<byte> memory, uint val)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(memory.Span, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt(IBufferWriter<byte> wrt, uint val)
    {
        var span = wrt.GetSpan(sizeof(uint));
        WriteUInt(ref span, val);
        wrt.Advance(sizeof(uint));
    }

    #endregion

    #region ReadUInt

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadUInt(Stream stream, ref uint value)
    {
        Span<byte> span = stackalloc byte[sizeof(uint)];
        stream.ReadExactly(span);
        value = BinaryPrimitives.ReadUInt32LittleEndian(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadUInt(ref ReadOnlySpan<byte> span)
    {
        var result = BinaryPrimitives.ReadUInt32LittleEndian(span);

        // 'Advance' the span.
        span = span[sizeof(uint)..];
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadUInt(ref ReadOnlySpan<byte> span, ref uint value)
    {
        value = BinaryPrimitives.ReadUInt32LittleEndian(span);

        // 'Advance' the span.
        span = span[sizeof(uint)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadUInt(ReadOnlySpan<byte> span, ref uint value)
    {
        value = BinaryPrimitives.ReadUInt32LittleEndian(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadUInt(ref ReadOnlyMemory<byte> span, ref uint value)
    {
        value = BinaryPrimitives.ReadUInt32LittleEndian(span.Span);

        // 'Advance' the span.
        span = span[sizeof(uint)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadUInt(ReadOnlyMemory<byte> span, ref uint value)
    {
        value = BinaryPrimitives.ReadUInt32LittleEndian(span.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadUInt(ref SequenceReader<byte> reader, ref uint value)
    {
        const int size = sizeof(uint);

        // Not enough data available; do not advance the reader.
        if (reader.Remaining < size)
        {
            return false;
        }

        // Fast path: all required bytes are in the current unread span.
        if (reader.UnreadSpan.Length >= size)
        {
            var ro = reader.UnreadSpan.Slice(0, size);
            ReadUInt(ref ro, ref value);
            reader.Advance(size);
            return true;
        }

        // Fallback: data crosses segment boundary — copy to a stack buffer.
        Span<byte> buf = stackalloc byte[size];
        if (!reader.TryCopyTo(buf))
        {
            return false; // Safety net (Remaining check should guarantee success)
        }

        reader.Advance(size);
        ReadOnlySpan<byte> tmp = buf;
        ReadUInt(ref tmp, ref value);
        return true;
    }

    #endregion
}
