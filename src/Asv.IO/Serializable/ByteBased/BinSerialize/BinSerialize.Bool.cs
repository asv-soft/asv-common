using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;

namespace Asv.IO;

public partial class BinSerialize
{
    /// <summary>
    /// 'Reserve' space for a boolean and return a ref to the space.
    /// </summary>
    /// <remarks>
    /// Will consume 1 byte.
    /// </remarks>
    /// <param name="span">Span to reserver from.</param>
    /// <returns>Reference to the reserved space.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref bool ReserveBool(ref Span<byte> span)
    {
        ref var result = ref Unsafe.As<byte, bool>(ref span[0]);

        // Init to default, as otherwise it would be whatever data was at that memory.
        result = default;

        // 'Advance' the span.
        span = span[sizeof(bool)..];

        return ref result;
    }

    #region ReadBool

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ReadBool(Stream stream)
    {
        return stream.ReadByte() != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadBool(Stream stream, ref bool value)
    {
        value = stream.ReadByte() != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ReadBool(ref ReadOnlySpan<byte> span)
    {
        var result = span[0] != 0;

        // 'Advance' the span.
        span = span[sizeof(bool)..];
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadBool(ref ReadOnlySpan<byte> span, ref bool value)
    {
        value = span[0] != 0;

        // 'Advance' the span.
        span = span[sizeof(bool)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadBool(ReadOnlySpan<byte> span, ref bool value)
    {
        value = span[0] != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadBool(ref ReadOnlyMemory<byte> memory, ref bool value)
    {
        value = memory.Span[0] != 0;

        // 'Advance' the span.
        memory = memory[sizeof(bool)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadBool(ReadOnlyMemory<byte> memory, ref bool value)
    {
        value = memory.Span[0] != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadBool(ref SequenceReader<byte> reader, ref bool value)
    {
        if (reader.TryRead(out var result))
        {
            value = result != 0;
            return true;
        }

        return false;
    }

    #endregion

    #region WriteBool

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBool(Stream stream, bool val)
    {
        stream.WriteByte((byte)(val ? 1 : 0));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBool(ref Span<byte> span, bool val)
    {
        span[0] = (byte)(val ? 1 : 0);

        // 'Advance' the span.
        span = span[sizeof(bool)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBool(Span<byte> span, bool val)
    {
        span[0] = (byte)(val ? 1 : 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBool(ref Memory<byte> memory, bool val)
    {
        memory.Span[0] = (byte)(val ? 1 : 0);

        // 'Advance' the span.
        memory = memory[sizeof(bool)..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBool(Memory<byte> memory, bool val)
    {
        memory.Span[0] = (byte)(val ? 1 : 0);
    }

    public static void WriteBool(IBufferWriter<byte> wrt, bool val)
    {
        var span = wrt.GetSpan(1);
        span[0] = (byte)(val ? 1 : 0);
        wrt.Advance(1);
    }

    #endregion
}
