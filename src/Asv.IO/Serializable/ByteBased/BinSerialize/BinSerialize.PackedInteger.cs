using System;
using System.Buffers;
using System.IO;

namespace Asv.IO;

public static partial class BinSerialize
{
    public static void ReadPackedInteger(Stream stream, ref int value)
    {
        var zigzagged = ReadPackedUnsignedInteger(stream);
        value = FromZigZagEncoding(zigzagged);
    }

    public static int ReadPackedInteger(Stream stream)
    {
        var zigzagged = ReadPackedUnsignedInteger(stream);
        return FromZigZagEncoding(zigzagged);
    }

    /// <summary>
    /// Read a packed integer.
    /// </summary>
    /// <param name="span">Span to read from.</param>
    /// <returns>Unpacked integer.</returns>
    public static int ReadPackedInteger(ref ReadOnlySpan<byte> span)
    {
        var zigzagged = ReadPackedUnsignedInteger(ref span);
        return FromZigZagEncoding(zigzagged);
    }

    /// <summary>
    /// Read a packed integer. https://en.wikipedia.org/wiki/Variable-length_quantity
    /// </summary>
    public static int ReadPackedInteger(ref ReadOnlyMemory<byte> span)
    {
        var zigzagged = ReadPackedUnsignedInteger(ref span);
        return FromZigZagEncoding(zigzagged);
    }

    /// <summary>
    /// Read a packed integer. https://en.wikipedia.org/wiki/Variable-length_quantity
    /// </summary>
    public static void ReadPackedInteger(ref ReadOnlySpan<byte> span, ref int value)
    {
        var zigzagged = ReadPackedUnsignedInteger(ref span);
        value = FromZigZagEncoding(zigzagged);
    }

    /// <summary>
    /// Read a packed integer. https://en.wikipedia.org/wiki/Variable-length_quantity
    /// </summary>
    public static void ReadPackedInteger(ref ReadOnlyMemory<byte> span, ref int value)
    {
        var zigzagged = ReadPackedUnsignedInteger(ref span);
        value = FromZigZagEncoding(zigzagged);
    }

    /// <summary>
    /// Read a packed integer. https://en.wikipedia.org/wiki/Variable-length_quantity
    /// </summary>
    public static bool ReadPackedInteger(ref SequenceReader<byte> reader, ref int value)
    {
        var zigzagged = 0U;
        if (TryReadPackedUnsignedInteger(ref reader, ref zigzagged) == false) return false;
        value = FromZigZagEncoding(zigzagged);
        return true;

    }
        
    /// <summary>
    /// Check how many bytes it will take to write the given value as a packed integer.
    /// </summary>
    /// <remarks>
    /// See <see cref="WritePackedInteger"/> for more information (including a size-table).
    /// </remarks>
    /// <param name="value">Value to check.</param>
    /// <returns>Number of bytes it will take.</returns>
    public static int GetSizeForPackedInteger(int value)
    {
        var zigzagged = ToZigZagEncoding(value);
        return GetSizeForPackedUnsignedInteger(zigzagged);
    }

    public static void WritePackedInteger(Stream stream, int value)
    {
        var zigzagged = ToZigZagEncoding(value);
        WritePackedUnsignedInteger(stream, zigzagged);
    }
    /// <summary>
    /// Pack a integer and write it.
    /// Uses a variable-length encoding scheme.
    /// </summary>
    /// <remarks>
    /// Size table:
    /// less then -134217729 = 5 bytes
    /// -134217728 to -1048577 = 4 bytes
    /// -1048576 to -8193 = 3 bytes
    /// -8192 to -65 = 2 bytes
    /// -64 to 63 = 1 bytes
    /// 64 to 8191 = 2 bytes
    /// 8192 to 1048575 = 3 bytes
    /// 1048576 to 134217727 = 4 bytes
    /// more then 134217728 = 5 bytes
    /// </remarks>
    /// <param name="span">Span to write to.</param>
    /// <param name="value">Value to pack and write.</param>
    public static void WritePackedInteger(ref Span<byte> span, int value)
    {
        var zigzagged = ToZigZagEncoding(value);
        WritePackedUnsignedInteger(ref span, zigzagged);
    }
        
    public static void WritePackedInteger(ref Memory<byte> span, int value)
    {
        var zigzagged = ToZigZagEncoding(value);
        WritePackedUnsignedInteger(ref span, zigzagged);
    }
}