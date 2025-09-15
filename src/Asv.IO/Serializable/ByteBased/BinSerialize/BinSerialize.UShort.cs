using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace Asv.IO;

public partial class BinSerialize
{
    /// <summary>
    /// 'Reserve' space for a unsigned 16 bit integer.
    /// </summary>
    /// <remarks>
    /// Will consume 2 bytes.
    /// </remarks>
    /// <param name="span">Span to reserver from.</param>
    /// <returns>Reference to the reserved space.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref ushort ReserveUShort(ref Span<byte> span)
    {
        ref var result = ref Unsafe.As<byte, ushort>(ref span[0]);

        // Init to default, as otherwise it would be whatever data was at that memory.
        result = 0;

        // 'Advance' the span.
        span = span[sizeof(ushort)..];
        return ref result;
    }

    #region ReadUShort

    public static void ReadUShort(Stream stream, ref ushort value)
    {
        Span<byte> span = stackalloc byte[sizeof(ushort)];
        stream.ReadExactly(span);
        value = BinaryPrimitives.ReadUInt16LittleEndian(span);
    }

    public static ushort ReadUShort(Stream stream)
    {
        Span<byte> span = stackalloc byte[sizeof(ushort)];
        stream.ReadExactly(span);
        return BinaryPrimitives.ReadUInt16LittleEndian(span);
    }

    /// <summary>
    /// Read a unsigned 16 bit integer.
    /// </summary>
    /// <remarks>
    /// Will consume 2 bytes.
    /// </remarks>
    /// <param name="span">Span to read from.</param>
    /// <returns>Read value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadUShort(ref ReadOnlySpan<byte> span)
    {
        var result = BinaryPrimitives.ReadUInt16LittleEndian(span);
        span = span[sizeof(ushort)..];
        return result;
    }

    /// <summary>
    /// Read a unsigned 16 bit integer.
    /// </summary>
    /// <remarks>
    /// Will consume 2 bytes.
    /// </remarks>
    /// <param name="span">Span to read from.</param>
    /// <param name="value">Read value</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadUShort(ref ReadOnlySpan<byte> span, ref ushort value)
    {
        value = BinaryPrimitives.ReadUInt16LittleEndian(span);
        span = span[sizeof(short)..];
    }

    /// <summary>
    /// Read a unsigned 16 bit integer.
    /// </summary>
    /// <remarks>
    /// Will consume 2 bytes.
    /// </remarks>
    /// <param name="span">Span to read from.</param>
    /// <param name="value">Read value</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadUShort(ReadOnlySpan<byte> span, ref ushort value)
    {
        value = BinaryPrimitives.ReadUInt16LittleEndian(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadUShort(ref ReadOnlyMemory<byte> span, ref ushort value)
    {
        value = BinaryPrimitives.ReadUInt16LittleEndian(span.Span);
        span = span[sizeof(ushort)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadUShort(ReadOnlyMemory<byte> span, ref ushort value)
    {
        value = BinaryPrimitives.ReadUInt16LittleEndian(span.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadUShort(ref SequenceReader<byte> reader, ref ushort value)
    {
        const int size = sizeof(ushort);

        // Not enough data available; do not advance the reader.
        if (reader.Remaining < size)
        {
            return false;
        }

        // Fast path: the required bytes are in the current unread span.
        if (reader.UnreadSpan.Length >= size)
        {
            var ro = reader.UnreadSpan.Slice(0, size);
            ReadUShort(ref ro, ref value);
            reader.Advance(size);
            return true;
        }

        // Fallback: copy across segments into a stack buffer.
        Span<byte> buf = stackalloc byte[size];
        if (!reader.TryCopyTo(buf))
        {
            return false; // Safety net, should not happen after Remaining check.
        }

        reader.Advance(size);
        ReadOnlySpan<byte> tmp = buf;
        ReadUShort(ref tmp, ref value);
        return true;
    }

    #endregion

    #region WriteUShort

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUShort(Stream stream, ushort val)
    {
        Span<byte> span = stackalloc byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16LittleEndian(span, val);
        stream.Write(span);
    }

    /// <summary>
    /// Write a 16 bit unsigned integer.
    /// </summary>
    /// <remarks>
    /// Will consume 2 bytes.
    /// </remarks>
    /// <param name="span">Span to write to.</param>
    /// <param name="val">Value to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUShort(ref Span<byte> span, ushort val)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(span, val);

        // 'Advance' the span.
        span = span[sizeof(ushort)..];
    }

    /// <summary>
    /// Write a 16 bit unsigned integer.
    /// </summary>
    /// <remarks>
    /// Will consume 2 bytes.
    /// </remarks>
    /// <param name="span">Span to write to.</param>
    /// <param name="val">Value to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUShort(Span<byte> span, ushort val)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(span, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUShort(IBufferWriter<byte> wrt, ushort val)
    {
        var span = wrt.GetSpan(sizeof(ushort));
        WriteUShort(ref span, val);
        wrt.Advance(sizeof(ushort));
    }

    #endregion
}
