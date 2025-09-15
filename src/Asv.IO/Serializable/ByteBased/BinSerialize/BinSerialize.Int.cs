using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Asv.IO;

public partial class BinSerialize
{
    /// <summary>
    /// 'Reserve' space for a signed 32 bit integer.
    /// </summary>
    /// <remarks>
    /// Will consume 4 bytes.
    /// </remarks>
    /// <param name="span">Span to reserver from.</param>
    /// <returns>Reference to the reserved space.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref int ReserveInt(ref Span<byte> span)
    {
        ref var result = ref Unsafe.As<byte, int>(ref span[0]);

        // Init to default, as otherwise it would be whatever data was at that memory.
        result = 0;

        // 'Advance' the span.
        span = span[sizeof(int)..];
        return ref result;
    }

    #region WriteInt

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteInt(Stream stream, in int value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteInt(Stream stream, int value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteInt(ref Span<byte> span, int val)
    {
        BinaryPrimitives.WriteInt32LittleEndian(span, val);

        // 'Advance' the span.
        span = span[sizeof(int)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteInt(Span<byte> span, int val)
    {
        BinaryPrimitives.WriteInt32LittleEndian(span, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteInt(ref Memory<byte> mem, in int val)
    {
        BinaryPrimitives.WriteInt32LittleEndian(mem.Span, val);

        // 'Advance' the span.
        mem = mem[sizeof(int)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteInt(Memory<byte> mem, in int val)
    {
        BinaryPrimitives.WriteInt32LittleEndian(mem.Span, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteInt(IBufferWriter<byte> wrt, int val)
    {
        var span = wrt.GetSpan(sizeof(int));
        WriteInt(ref span, val);
        wrt.Advance(sizeof(int));
    }

    #endregion

    #region ReadInt

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadInt(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadInt32LittleEndian(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadInt(Stream stream, ref int value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        stream.ReadExactly(buffer);
        value = BinaryPrimitives.ReadInt32LittleEndian(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadInt(ref ReadOnlySpan<byte> span)
    {
        var result = BinaryPrimitives.ReadInt32LittleEndian(span);

        // 'Advance' the span.
        span = span[sizeof(int)..];

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadInt(ref ReadOnlySpan<byte> span, ref int value)
    {
        value = BinaryPrimitives.ReadInt32LittleEndian(span);

        // 'Advance' the span.
        span = span[sizeof(int)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadInt(ReadOnlySpan<byte> span, ref int value)
    {
        value = BinaryPrimitives.ReadInt32LittleEndian(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadInt(ReadOnlyMemory<byte> memory, ref int value)
    {
        value = BinaryPrimitives.ReadInt32LittleEndian(memory.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadInt(ref ReadOnlyMemory<byte> memory, ref int value)
    {
        value = BinaryPrimitives.ReadInt32LittleEndian(memory.Span);

        // 'Advance' the span.
        memory = memory[sizeof(int)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadInt(ref SequenceReader<byte> reader, ref int value)
    {
        const int size = sizeof(int);

        // Not enough data; do not advance the reader.
        if (reader.Remaining < size)
        {
            return false;
        }

        // Fast path: all bytes are in the current unread span.
        if (reader.UnreadSpan.Length >= size)
        {
            var ro = reader.UnreadSpan.Slice(0, size);
            ReadInt(ref ro, ref value);
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
        ReadInt(ref tmp, ref value);
        return true;
    }

    #endregion
}
