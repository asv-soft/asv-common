using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;

namespace Asv.IO;

public partial class BinSerialize
{
    #region ReadBitRange

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Read8BitRange(Stream stream, in float min, in float max)
    {
        // Read a byte.
        var raw = stream.ReadByte();
        if (raw == -1)
        {
            throw new EndOfStreamException();
        }

        // Remap it to the given range.
        return Interpolate(min, max, (float)raw / byte.MaxValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Read8BitRange(Stream stream, in float min, in float max, ref float value)
    {
        // Read a byte.
        var raw = stream.ReadByte();
        if (raw == -1)
        {
            throw new EndOfStreamException();
        }

        // Remap it to the given range.
        value = Interpolate(min, max, (float)raw / byte.MaxValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Read8BitRange(ref ReadOnlySpan<byte> span, in float min, in float max)
    {
        // Read a byte.
        var raw = ReadByte(ref span);

        // Remap it to the given range.
        return Interpolate(min, max, (float)raw / byte.MaxValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Read8BitRange(ref ReadOnlyMemory<byte> span, in float min, in float max)
    {
        // Read a byte.
        var raw = ReadByte(ref span);

        // Remap it to the given range.
        return Interpolate(min, max, (float)raw / byte.MaxValue);
    }

    public static bool TryRead8BitRange(
        ref SequenceReader<byte> rdr,
        in float min,
        in float max,
        ref float value
    )
    {
        byte raw = 0;
        if (TryReadByte(ref rdr, ref raw) == false)
        {
            return false;
        }

        value = Interpolate(min, max, (float)raw / byte.MaxValue);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Read16BitRange(Stream stream, in float min, in float max)
    {
        var raw = ReadUShort(stream);
        return Interpolate(min, max, (float)raw / ushort.MaxValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Read16BitRange(ref ReadOnlySpan<byte> span, in float min, in float max)
    {
        // Read a ushort.
        var raw = ReadUShort(ref span);

        // Remap it to the given range.
        return Interpolate(min, max, (float)raw / ushort.MaxValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Read16BitRange(ref ReadOnlyMemory<byte> span, in float min, in float max)
    {
        ushort raw = 0;
        ReadUShort(ref span, ref raw);

        // Remap it to the given range.
        return Interpolate(min, max, (float)raw / ushort.MaxValue);
    }

    public static bool TryRead16BitRange(
        ref SequenceReader<byte> rdr,
        in float min,
        in float max,
        ref float value
    )
    {
        ushort raw = 0;
        if (TryReadUShort(ref rdr, ref raw) == false)
        {
            return false;
        }

        value = Interpolate(min, max, (float)raw / ushort.MaxValue);
        return true;
    }

    #endregion

    #region WriteBitRange

    /// <summary>
    /// Writes a value mapped to the specified range using 8-bit precision.
    /// The range [min..max] is quantized into 256 discrete steps (0–255).
    /// Consumes 1 byte.
    /// </summary>
    /// <param name="stream">Destination stream where the encoded value will be written.</param>
    /// <param name="min">Minimum value of the range.</param>
    /// <param name="max">Maximum value of the range.</param>
    /// <param name="val">Value to encode (must be within the specified range).</param>
    public static void Write8BitRange(Stream stream, in float min, in float max, in float val)
    {
        var frac = Fraction(min, max, val);
        stream.WriteByte((byte)((byte.MaxValue * frac) + .5f));
    }

    /// <summary>
    /// Writes a value mapped to the specified range into a <see cref="Span{T}"/>
    /// using 8-bit precision.
    /// The range [min..max] is quantized into 256 discrete steps (0–255).
    /// </summary>
    /// <remarks>
    /// Consumes exactly 1 byte.
    /// </remarks>
    /// <param name="span">Destination span to write to (will be advanced by 1 byte).</param>
    /// <param name="min">Minimum value of the allowed range.</param>
    /// <param name="max">Maximum value of the allowed range.</param>
    /// <param name="val">Value to encode (should be within [min..max]).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write8BitRange(ref Span<byte> span, in float min, in float max, in float val)
    {
        var frac = Fraction(min, max, val);
        WriteByte(ref span, (byte)((byte.MaxValue * frac) + .5f));
    }

    /// <summary>
    /// Writes a value mapped to the specified range into an <see cref="IBufferWriter{T}"/>
    /// using 8-bit precision.
    /// The range [min..max] is quantized into 256 discrete steps (0–255).
    /// </summary>
    /// <remarks>
    /// Consumes exactly 1 byte.
    /// </remarks>
    /// <param name="wrt">Destination buffer writer.</param>
    /// <param name="min">Minimum value of the allowed range.</param>
    /// <param name="max">Maximum value of the allowed range.</param>
    /// <param name="val">Value to encode (should be within [min..max]).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write8BitRange(
        IBufferWriter<byte> wrt,
        in float min,
        in float max,
        in float val
    )
    {
        var frac = Fraction(min, max, val);
        WriteByte(wrt, (byte)((byte.MaxValue * frac) + .5f));
    }

    /// <summary>
    /// Writes a value mapped to the specified range into a <see cref="Stream"/>
    /// using 16-bit precision.
    /// The range [min..max] is quantized into 65,536 discrete steps (0–65535).
    /// </summary>
    /// <remarks>
    /// Consumes exactly 2 bytes.
    /// </remarks>
    /// <param name="stream">Destination stream to write the encoded value.</param>
    /// <param name="min">Minimum value of the allowed range.</param>
    /// <param name="max">Maximum value of the allowed range.</param>
    /// <param name="val">Value to encode (should be within [min..max]).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write16BitRange(Stream stream, in float min, in float max, in float val)
    {
        var frac = Fraction(min, max, val);
        WriteUShort(stream, (ushort)((ushort.MaxValue * frac) + .5f));
    }

    /// <summary>
    /// Writes a value mapped to the specified range into a <see cref="Span{T}"/>
    /// using 16-bit precision.
    /// The range [min..max] is quantized into 65,536 discrete steps (0–65535).
    /// </summary>
    /// <remarks>
    /// Consumes exactly 2 bytes.
    /// </remarks>
    /// <param name="span">Destination span to write to (will be advanced by 2 bytes).</param>
    /// <param name="min">Minimum value of the allowed range.</param>
    /// <param name="max">Maximum value of the allowed range.</param>
    /// <param name="val">Value to encode (should be within [min..max]).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write16BitRange(
        ref Span<byte> span,
        in float min,
        in float max,
        in float val
    )
    {
        var frac = Fraction(min, max, val);
        WriteUShort(ref span, (ushort)((ushort.MaxValue * frac) + .5f));
    }

    /// <summary>
    /// Writes a value mapped to the specified range into an <see cref="IBufferWriter{T}"/>
    /// using 16-bit precision.
    /// The range [min..max] is quantized into 65,536 discrete steps (0–65535).
    /// </summary>
    /// <remarks>
    /// Consumes exactly 2 bytes.
    /// </remarks>
    /// <param name="wrt">Destination buffer writer.</param>
    /// <param name="min">Minimum value of the allowed range.</param>
    /// <param name="max">Maximum value of the allowed range.</param>
    /// <param name="val">Value to encode (should be within [min..max]).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write16BitRange(
        IBufferWriter<byte> wrt,
        in float min,
        in float max,
        in float val
    )
    {
        var frac = Fraction(min, max, val);
        WriteUShort(wrt, (ushort)((ushort.MaxValue * frac) + .5f));
    }

    #endregion
}
