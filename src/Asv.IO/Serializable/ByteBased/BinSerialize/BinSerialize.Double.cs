using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace Asv.IO;

public partial class BinSerialize
{
    #region WriteDouble

    public static void WriteDouble(Stream stream, in double val)
    {
        Span<byte> span = stackalloc byte[sizeof(double)];
        BinaryPrimitives.WriteDoubleLittleEndian(span, val);
        stream.Write(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDouble(ref Span<byte> span, double val)
    {
        BinaryPrimitives.WriteDoubleLittleEndian(span, val);

        // 'Advance' the span.
        span = span[sizeof(double)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDouble(ref Span<byte> span, in double val)
    {
        BinaryPrimitives.WriteDoubleLittleEndian(span, val);

        // 'Advance' the span.
        span = span[sizeof(double)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDouble(Span<byte> span, in double val)
    {
        BinaryPrimitives.WriteDoubleLittleEndian(span, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDouble(ref Memory<byte> memory, in double val)
    {
        BinaryPrimitives.WriteDoubleLittleEndian(memory.Span, val);

        // 'Advance' the span.
        memory = memory[sizeof(double)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDouble(Memory<byte> memory, in double val)
    {
        BinaryPrimitives.WriteDoubleLittleEndian(memory.Span, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDouble(IBufferWriter<byte> wrt, in double val)
    {
        var span = wrt.GetSpan(sizeof(double));
        WriteDouble(ref span, in val);
        wrt.Advance(sizeof(double));
    }

    #endregion

    #region ReadDouble

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadDouble(Stream stream, ref double value)
    {
        Span<byte> span = stackalloc byte[sizeof(double)];
        stream.ReadExactly(span);
        value = BinaryPrimitives.ReadDoubleLittleEndian(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ReadDouble(Stream stream)
    {
        Span<byte> span = stackalloc byte[sizeof(double)];
        stream.ReadExactly(span);
        return BinaryPrimitives.ReadDoubleLittleEndian(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ReadDouble(ref ReadOnlySpan<byte> span)
    {
        var result = BinaryPrimitives.ReadDoubleLittleEndian(span);

        // 'Advance' the span.
        span = span[sizeof(double)..];
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadDouble(ref ReadOnlySpan<byte> span, ref double value)
    {
        value = BinaryPrimitives.ReadDoubleLittleEndian(span);

        // 'Advance' the span.
        span = span[sizeof(double)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadDouble(ReadOnlySpan<byte> span, ref double value)
    {
        value = BinaryPrimitives.ReadDoubleLittleEndian(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadDouble(ref ReadOnlyMemory<byte> span, ref double value)
    {
        value = BinaryPrimitives.ReadDoubleLittleEndian(span.Span);

        // 'Advance' the span.
        span = span[sizeof(double)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadDouble(ReadOnlyMemory<byte> span, ref double value)
    {
        value = BinaryPrimitives.ReadDoubleLittleEndian(span.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadDouble(ref SequenceReader<byte> reader, ref double value)
    {
        const int size = sizeof(double);

        if (reader.Remaining < size)
        {
            return false;
        }

        Span<byte> buf = stackalloc byte[size];

        if (!reader.TryCopyTo(buf))
        {
            return false;
        }

        reader.Advance(size);

        ReadOnlySpan<byte> ro = buf;
        ReadDouble(ref ro, ref value);

        return true;
    }

    #endregion
}
