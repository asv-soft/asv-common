using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;

namespace Asv.IO;

public partial class BinSerialize
{
    /// <summary>
    /// 'Reserve' space for a signed 8 bit integer.
    /// </summary>
    /// <remarks>
    /// Will consume 1 byte.
    /// </remarks>
    /// <param name="span">Span to reserver from.</param>
    /// <returns>Reference to the reserved space.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref sbyte ReserveSByte(ref Span<byte> span)
    {
        ref var result = ref Unsafe.As<byte, sbyte>(ref span[0]);

        // Init to default, as otherwise it would be whatever data was at that memory.
        result = default;

        // 'Advance' the span.
        span = span[sizeof(sbyte)..];
        return ref result;
    }

    #region WriteSByte

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSByte(Stream stream, sbyte val)
    {
        stream.WriteByte((byte)val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSByte(ref Span<byte> span, sbyte val)
    {
        span[0] = (byte)val;

        // 'Advance' the span.
        span = span[sizeof(sbyte)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSByte(Span<byte> span, sbyte val)
    {
        span[0] = (byte)val;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSByte(ref Memory<byte> memory, sbyte val)
    {
        memory.Span[0] = (byte)val;

        // 'Advance' the span.
        memory = memory[sizeof(sbyte)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSByte(Memory<byte> memory, sbyte val)
    {
        memory.Span[0] = (byte)val;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSByte(IBufferWriter<byte> wrt, in sbyte val)
    {
        var span = wrt.GetSpan(1);
        WriteSByte(ref span, val);
        wrt.Advance(1);
    }

    #endregion

    #region ReadSByte

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte ReadSByte(Stream stream)
    {
        var result = stream.ReadByte();
        if (result == -1)
        {
            throw new EndOfStreamException("Reached end of stream while trying to read a sbyte");
        }

        return (sbyte)result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte ReadSByte(ref ReadOnlySpan<byte> span)
    {
        var result = (sbyte)span[0];

        // 'Advance' the span.
        span = span[sizeof(sbyte)..];
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadSByte(ref ReadOnlySpan<byte> span, ref sbyte value)
    {
        value = (sbyte)span[0];
        span = span[sizeof(sbyte)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadSByte(ReadOnlySpan<byte> span, ref sbyte value)
    {
        value = (sbyte)span[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadSByte(ref ReadOnlyMemory<byte> memory, ref sbyte value)
    {
        value = (sbyte)memory.Span[0];
        memory = memory[sizeof(sbyte)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadSByte(ReadOnlyMemory<byte> memory, ref sbyte value)
    {
        value = (sbyte)memory.Span[0];
    }

    public static bool TryReadSByte(ref SequenceReader<byte> reader, ref sbyte value)
    {
        if (reader.TryRead(out var result))
        {
            value = (sbyte)result;
            return true;
        }

        return false;
    }

    #endregion
}
