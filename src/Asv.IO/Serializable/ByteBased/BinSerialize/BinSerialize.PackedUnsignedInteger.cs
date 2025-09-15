using System;
using System.Buffers;
using System.IO;

namespace Asv.IO;

public static partial class BinSerialize
{
    #region Read Packed Unsigned Integer

    /// <summary>
    /// Read a packed unsigned integer. https://en.wikipedia.org/wiki/Variable-length_quantity
    /// </summary>
    public static uint ReadPackedUnsignedInteger(Stream stream)
    {
        uint result = 0;
        var shift = 0;
        for (var i = 0; i < 5; i++)
        {
            var b = stream.ReadByte();
            if (b < 0)
            {
                throw new EndOfStreamException("EOF while reading VLQ UInt32");
            }

            result |= (uint)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                return result;
            }

            shift += 7;
        }
        throw new FormatException("VLQ UInt32 overflow/malformed");
    }

    /// <summary>
    /// Read a packed unsigned integer. https://en.wikipedia.org/wiki/Variable-length_quantity
    /// </summary>
    public static uint ReadPackedUnsignedInteger(ref ReadOnlySpan<byte> span)
    {
        /* Read 7 bits of integer data and then the 8th bit indicates wether more data will follow.
        More info: https://en.wikipedia.org/wiki/Variable-length_quantity */

        uint result = 0;
        var resultBitOffset = 0;
        while (true)
        {
            var data = ReadByte(ref span);

            // Mask of the first 7 bits of the data and then 'apply' it to the result.
            result |= (uint)(data & 0b0111_1111) << resultBitOffset;

            // Check the last bit to see if this was the end.
            if ((data & 0b1000_0000) == 0)
            {
                break;
            }

            // Increment the offset so the next iteration points at the next bits of the result.
            resultBitOffset += 7;
        }

        return result;
    }

    /// <summary>
    /// Read a packed unsigned integer. https://en.wikipedia.org/wiki/Variable-length_quantity
    /// </summary>
    public static uint ReadPackedUnsignedInteger(ref ReadOnlyMemory<byte> span)
    {
        /* Read 7 bits of integer data and then the 8th bit indicates wether more data will follow.
        More info: https://en.wikipedia.org/wiki/Variable-length_quantity */

        uint result = 0;
        var resultBitOffset = 0;
        while (true)
        {
            var data = ReadByte(ref span);

            // Mask of the first 7 bits of the data and then 'apply' it to the result.
            result |= (uint)(data & 0b0111_1111) << resultBitOffset;

            // Check the last bit to see if this was the end.
            if ((data & 0b1000_0000) == 0)
            {
                break;
            }

            // Increment the offset so the next iteration points at the next bits of the result.
            resultBitOffset += 7;
        }

        return result;
    }

    /// <summary>
    /// Read a packed unsigned integer. https://en.wikipedia.org/wiki/Variable-length_quantity
    /// </summary>
    public static void ReadPackedUnsignedInteger(ref ReadOnlySpan<byte> span, ref uint value)
    {
        /* Read 7 bits of integer data and then the 8th bit indicates wether more data will follow.
        More info: https://en.wikipedia.org/wiki/Variable-length_quantity */

        value = 0;
        var resultBitOffset = 0;
        while (true)
        {
            var data = ReadByte(ref span);

            // Mask of the first 7 bits of the data and then 'apply' it to the result.
            value |= (uint)(data & 0b0111_1111) << resultBitOffset;

            // Check the last bit to see if this was the end.
            if ((data & 0b1000_0000) == 0)
            {
                break;
            }

            // Increment the offset so the next iteration points at the next bits of the result.
            resultBitOffset += 7;
        }
    }

    /// <summary>
    /// Read a packed unsigned integer. https://en.wikipedia.org/wiki/Variable-length_quantity
    /// </summary>
    public static void ReadPackedUnsignedInteger(ref ReadOnlyMemory<byte> span, ref uint value)
    {
        /* Read 7 bits of integer data and then the 8th bit indicates wether more data will follow.
        More info: https://en.wikipedia.org/wiki/Variable-length_quantity */

        value = 0;
        var resultBitOffset = 0;
        while (true)
        {
            var data = ReadByte(ref span);

            // Mask of the first 7 bits of the data and then 'apply' it to the result.
            value |= (uint)(data & 0b0111_1111) << resultBitOffset;

            // Check the last bit to see if this was the end.
            if ((data & 0b1000_0000) == 0)
            {
                break;
            }

            // Increment the offset so the next iteration points at the next bits of the result.
            resultBitOffset += 7;
        }
    }

    /// <summary>
    /// Read a packed unsigned integer. https://en.wikipedia.org/wiki/Variable-length_quantity
    /// </summary>
    public static bool TryReadPackedUnsignedInteger(
        ref SequenceReader<byte> reader,
        ref uint value,
        out int consumed
    )
    {
        value = 0;
        consumed = 0;
        var resultBitOffset = 0;
        while (true)
        {
            if (!reader.TryRead(out byte data))
            {
                value = 0;

                reader.Rewind(consumed);
                return false; // Not enough data to read the entire integer
            }

            consumed++;
            value |= (uint)(data & 0b0111_1111) << resultBitOffset;

            if ((data & 0b1000_0000) == 0)
            {
                return true; // Successfully read the packed unsigned integer
            }

            resultBitOffset += 7;

            // Prevent reading more than 5 bytes (UInt32 max size)
            if (resultBitOffset >= 35)
            {
                value = 0;
                reader.Rewind(consumed);
                return false; // Overflow or malformed data
            }
        }
    }

    /// <summary>
    /// Read a packed unsigned integer. https://en.wikipedia.org/wiki/Variable-length_quantity
    /// </summary>
    public static bool TryReadPackedUnsignedInteger(ref SequenceReader<byte> reader, ref uint value)
    {
        return TryReadPackedUnsignedInteger(ref reader, ref value, out _);
    }

    #endregion

    #region WritePackedUnsignedInteger

    /// <summary>
    /// Check how many bytes it will take to write the given value as a packed unsigned integer.
    /// </summary>
    /// <remarks>
    /// See <see cref="WritePackedUnsignedInteger"/> for more information (including a size-table).
    /// </remarks>
    /// <param name="value">Value to check.</param>
    /// <returns>Number of bytes it will take.</returns>
    public static int GetSizeForPackedUnsignedInteger(uint value)
    {
        /* Check how many 7 bit values we need to store the integer, for more info see
        'WritePackedUnsignedInteger' implementation. */

        var bytes = 1;
        while (value > 0b0111_1111)
        {
            value >>= 7;
            bytes++;
        }

        return bytes;
    }

    public static void WritePackedUnsignedInteger(Stream stream, uint value)
    {
        while (value > 0b0111_1111)
        {
            // Write out the value and set the 8th bit to 1 to indicate more data will follow.
            WriteByte(stream, (byte)(value | 0b1000_0000));

            // Shift the value by 7 to 'consume' the bits we've just written.
            value >>= 7;
        }

        // Write out the last data (the 8th bit will always be 0 here to indicate the end).
        WriteByte(stream, (byte)value);
    }

    /// <summary>
    /// Pack a unsigned integer and write it.
    /// Uses a variable-length encoding scheme.
    /// </summary>
    /// <remarks>
    /// Size table:
    /// 0 to 127 = 1 bytes
    /// 128 to 16383 = 2 bytes
    /// 16384 to 2097151 = 3 bytes
    /// 2097152 to 268435455 = 4 bytes
    /// more then 268435456 = 5 bytes
    /// </remarks>
    /// <param name="span">Span to write to.</param>
    /// <param name="value">Value to pack and write.</param>
    public static void WritePackedUnsignedInteger(ref Span<byte> span, uint value)
    {
        /* Write 7 bits of integer data and then the 8th bit indicates wether more data will follow.
        More info: https://en.wikipedia.org/wiki/Variable-length_quantity */

        // As long as we have more data left then we can fit into 7 bits we need to 'split' it up.
        while (value > 0b0111_1111)
        {
            // Write out the value and set the 8th bit to 1 to indicate more data will follow.
            WriteByte(ref span, (byte)(value | 0b1000_0000));

            // Shift the value by 7 to 'consume' the bits we've just written.
            value >>= 7;
        }

        // Write out the last data (the 8th bit will always be 0 here to indicate the end).
        WriteByte(ref span, (byte)value);
    }

    public static void WritePackedUnsignedInteger(ref Memory<byte> span, uint value)
    {
        /* Write 7 bits of integer data and then the 8th bit indicates wether more data will follow.
        More info: https://en.wikipedia.org/wiki/Variable-length_quantity */

        // As long as we have more data left then we can fit into 7 bits we need to 'split' it up.
        while (value > 0b0111_1111)
        {
            // Write out the value and set the 8th bit to 1 to indicate more data will follow.
            WriteByte(ref span, (byte)(value | 0b1000_0000));

            // Shift the value by 7 to 'consume' the bits we've just written.
            value >>= 7;
        }

        // Write out the last data (the 8th bit will always be 0 here to indicate the end).
        WriteByte(ref span, (byte)value);
    }

    public static void WritePackedUnsignedInteger(IBufferWriter<byte> wrt, uint value)
    {
        /* Write 7 bits of integer data and then the 8th bit indicates wether more data will follow.
        More info: https://en.wikipedia.org/wiki/Variable-length_quantity */

        // As long as we have more data left then we can fit into 7 bits we need to 'split' it up.
        while (value > 0b0111_1111)
        {
            // Write out the value and set the 8th bit to 1 to indicate more data will follow.
            WriteByte(wrt, (byte)(value | 0b1000_0000));

            // Shift the value by 7 to 'consume' the bits we've just written.
            value >>= 7;
        }

        // Write out the last data (the 8th bit will always be 0 here to indicate the end).
        WriteByte(wrt, (byte)value);
    }

    #endregion
}
