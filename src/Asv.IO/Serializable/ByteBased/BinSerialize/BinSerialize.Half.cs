using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace Asv.IO;

public partial class BinSerialize
{
    #region Write Half

    public static unsafe void WriteHalf(Stream stream, Half val)
    {
        Span<byte> buffer = stackalloc byte[sizeof(Half)];
        BinaryPrimitives.WriteHalfLittleEndian(buffer, val);
        stream.Write(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void WriteHalf(ref Span<byte> span, Half val)
    {
        BinaryPrimitives.WriteHalfLittleEndian(span, val);

        // 'Advance' the span.
        span = span[sizeof(Half)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void WriteHalf(ref Span<byte> span, in Half val)
    {
        BinaryPrimitives.WriteHalfLittleEndian(span, val);

        // 'Advance' the span.
        span = span[sizeof(Half)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteHalf(Span<byte> span, in Half val)
    {
        BinaryPrimitives.WriteHalfLittleEndian(span, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteHalf(Span<byte> span, Half val)
    {
        BinaryPrimitives.WriteHalfLittleEndian(span, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteHalf(Memory<byte> memory, in Half val)
    {
        BinaryPrimitives.WriteHalfLittleEndian(memory.Span, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void WriteHalf(ref Memory<byte> memory, in Half val)
    {
        BinaryPrimitives.WriteHalfLittleEndian(memory.Span, val);

        // 'Advance' the span.
        memory = memory[sizeof(Half)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void WriteHalf(IBufferWriter<byte> wrt, in Half val)
    {
        var span = wrt.GetSpan(sizeof(Half));
        WriteHalf(ref span, in val);
        wrt.Advance(sizeof(Half));
    }

    #endregion

    #region Read Half

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Half ReadHalf(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(Half)];
        stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadHalfLittleEndian(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void ReadHalf(Stream stream, ref Half value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(Half)];
        stream.ReadExactly(buffer);
        value = BinaryPrimitives.ReadHalfLittleEndian(buffer);
    }

    /// <summary>
    /// Read a 32 bit Halfing-point number.
    /// </summary>
    /// <remarks>
    /// Will consume 4 bytes.
    /// </remarks>
    /// <param name="span">Span to read from.</param>
    /// <returns>Read value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Half ReadHalf(ref ReadOnlySpan<byte> span)
    {
        var result = BinaryPrimitives.ReadHalfLittleEndian(span);

        // 'Advance' the span.
        span = span[sizeof(Half)..];
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void ReadHalf(ref ReadOnlySpan<byte> span, ref Half value)
    {
        value = BinaryPrimitives.ReadHalfLittleEndian(span);

        // 'Advance' the span.
        span = span[sizeof(Half)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadHalf(ReadOnlySpan<byte> span, ref Half value)
    {
        value = BinaryPrimitives.ReadHalfLittleEndian(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void ReadHalf(ref ReadOnlyMemory<byte> memory, ref Half value)
    {
        value = BinaryPrimitives.ReadHalfLittleEndian(memory.Span);

        // 'Advance' the span.
        memory = memory[sizeof(Half)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadHalf(ReadOnlyMemory<byte> memory, ref Half value)
    {
        value = BinaryPrimitives.ReadHalfLittleEndian(memory.Span);
    }

    public static unsafe bool TryReadHalf(ref SequenceReader<byte> reader, ref Half value)
    {
        var size = sizeof(Half);
        var buff = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            var span = new Span<byte>(buff, 0, size);
            if (reader.TryCopyTo(span) == false)
            {
                return false;
            }

            reader.Advance(size);
            var roSpan = new ReadOnlySpan<byte>(buff, 0, size);
            ReadHalf(ref roSpan, ref value);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buff);
        }

        return true;
    }

    #endregion
}
