using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;

namespace Asv.IO;

public partial class BinSerialize
{
    /// <summary>
    /// 'Reserve' space for a unsigned 8 bit integer.
    /// </summary>
    /// <remarks>
    /// Will consume 1 byte.
    /// </remarks>
    /// <param name="span">Span to reserver from.</param>
    /// <returns>Reference to the reserved space.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref byte ReserveByte(ref Span<byte> span)
    {
        ref var result = ref span[0];

        // Init to default, as otherwise it would be whatever data was at that memory.
        result = 0;

        // 'Advance' the span.
        span = span[sizeof(byte)..];
        return ref result;
    }

    #region ReadByte

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadByte(Stream stream)
    {
        var result = stream.ReadByte();
        if (result == -1)
        {
            throw new EndOfStreamException("Reached end of stream while trying to read a byte");
        }

        return (byte)result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadByte(Stream stream, ref byte value)
    {
        var result = stream.ReadByte();
        if (result == -1)
        {
            throw new EndOfStreamException("Reached end of stream while trying to read a byte");
        }

        value = (byte)result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadByte(ref ReadOnlySpan<byte> span)
    {
        var result = span[0];

        // 'Advance' the span.
        span = span[sizeof(byte)..];
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadByte(ref ReadOnlySpan<byte> span, ref byte value)
    {
        value = span[0];

        // 'Advance' the span.
        span = span[sizeof(byte)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadByte(ReadOnlySpan<byte> span, ref byte value)
    {
        value = span[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadByte(ref ReadOnlyMemory<byte> memory)
    {
        var result = memory.Span[0];

        // 'Advance' the span.
        memory = memory[sizeof(byte)..];
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadByte(ReadOnlyMemory<byte> memory, ref byte value)
    {
        value = memory.Span[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadByte(ref ReadOnlyMemory<byte> memory, ref byte value)
    {
        value = memory.Span[0];

        // 'Advance' the span.
        memory = memory[sizeof(byte)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadByte(ref SequenceReader<byte> reader, ref byte value)
    {
        return reader.TryRead(out value);
    }

    #endregion

    #region WriteByte

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteByte(Stream stream, byte val)
    {
        stream.WriteByte(val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteByte(ref Span<byte> span, byte val)
    {
        span[0] = val;

        // 'Advance' the span.
        span = span[sizeof(byte)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteByte(Span<byte> span, byte val)
    {
        span[0] = val;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteByte(ref Memory<byte> span, byte val)
    {
        span.Span[0] = val;

        // 'Advance' the span.
        span = span[sizeof(byte)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteByte(IBufferWriter<byte> wrt, byte val)
    {
        var span = wrt.GetSpan(1);
        span[0] = val;
        wrt.Advance(1);
    }

    #endregion
}
