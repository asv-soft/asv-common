using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace Asv.IO;

public partial class BinSerialize
{
    #region WriteFloat

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFloat(Stream stream, float val)
    {
        Span<byte> span = stackalloc byte[sizeof(float)];
        BinaryPrimitives.WriteSingleLittleEndian(span, val);
        stream.Write(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFloat(Stream stream, in float val)
    {
        Span<byte> span = stackalloc byte[sizeof(float)];
        BinaryPrimitives.WriteSingleLittleEndian(span, val);
        stream.Write(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFloat(ref Span<byte> span, float val)
    {
        BinaryPrimitives.WriteSingleLittleEndian(span, val);

        // 'Advance' the span.
        span = span[sizeof(float)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFloat(ref Span<byte> span, in float val)
    {
        BinaryPrimitives.WriteSingleLittleEndian(span, val);

        // 'Advance' the span.
        span = span[sizeof(float)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFloat(Span<byte> span, in float val)
    {
        BinaryPrimitives.WriteSingleLittleEndian(span, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFloat(Span<byte> span, float val)
    {
        BinaryPrimitives.WriteSingleLittleEndian(span, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFloat(Memory<byte> memory, in float val)
    {
        BinaryPrimitives.WriteSingleLittleEndian(memory.Span, val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFloat(ref Memory<byte> memory, in float val)
    {
        BinaryPrimitives.WriteSingleLittleEndian(memory.Span, val);

        // 'Advance' the span.
        memory = memory[sizeof(float)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFloat(IBufferWriter<byte> wrt, in float val)
    {
        var span = wrt.GetSpan(sizeof(float));
        WriteFloat(ref span, in val);
        wrt.Advance(sizeof(float));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFloat(IBufferWriter<byte> wrt, float val)
    {
        var span = wrt.GetSpan(sizeof(float));
        WriteFloat(ref span, in val);
        wrt.Advance(sizeof(float));
    }

    #endregion

    #region ReadFloat

    public static float ReadFloat(Stream stream)
    {
        Span<byte> span = stackalloc byte[sizeof(float)];
        stream.ReadExactly(span);
        return BinaryPrimitives.ReadSingleLittleEndian(span);
    }

    /// <summary>
    /// Read a 32 bit floating-point number.
    /// </summary>
    /// <remarks>
    /// Will consume 4 bytes.
    /// </remarks>
    /// <param name="span">Span to read from.</param>
    /// <returns>Read value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ReadFloat(ref ReadOnlySpan<byte> span)
    {
        var result = BinaryPrimitives.ReadSingleLittleEndian(span);

        // 'Advance' the span.
        span = span[sizeof(float)..];
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadFloat(ref ReadOnlySpan<byte> span, ref float value)
    {
        value = BinaryPrimitives.ReadSingleLittleEndian(span);

        // 'Advance' the span.
        span = span[sizeof(float)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadFloat(ReadOnlySpan<byte> span, ref float value)
    {
        value = BinaryPrimitives.ReadSingleLittleEndian(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadFloat(ref ReadOnlyMemory<byte> memory, ref float value)
    {
        value = BinaryPrimitives.ReadSingleLittleEndian(memory.Span);

        // 'Advance' the span.
        memory = memory[sizeof(float)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadFloat(ReadOnlyMemory<byte> memory, ref float value)
    {
        value = BinaryPrimitives.ReadSingleLittleEndian(memory.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadFloat(ref SequenceReader<byte> reader, ref float value)
    {
        const int size = sizeof(float);

        // Недостаточно данных — без продвижения курсора.
        if (reader.Remaining < size)
        {
            return false;
        }

        // Fast-path: данные полностью в текущем сегменте, читаем без копирования.
        if (reader.UnreadSpan.Length >= size)
        {
            var ro = reader.UnreadSpan.Slice(0, size);
            ReadFloat(ref ro, ref value);
            reader.Advance(size);
            return true;
        }

        // Fallback: данные пересекают границу сегментов — копируем в стек.
        Span<byte> buf = stackalloc byte[size];
        if (!reader.TryCopyTo(buf))
        {
            return false;
        }

        reader.Advance(size);
        ReadOnlySpan<byte> tmp = buf;
        ReadFloat(ref tmp, ref value);
        return true;
    }

    #endregion
}
