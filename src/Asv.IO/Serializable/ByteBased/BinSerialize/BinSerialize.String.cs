using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Asv.IO;

public static partial class BinSerialize
{
    /// <summary>
    /// Check how many bytes it will take to write the given string value.
    /// </summary>
    /// <remarks>
    /// Size is the UTF-8 byte count as a 'packed unsigned integer' plus the encoded bytes.
    /// </remarks>
    /// <param name="val">Value to get the size for.</param>
    /// <returns>Number of bytes it will take.</returns>
    public static int GetSizeForString(string? val)
    {
        unsafe
        {
            if (string.IsNullOrWhiteSpace(val))
            {
                return GetSizeForPackedUnsignedInteger(0);
            }

            fixed (char* charPointer = val)
            {
                return GetSizeForString(charPointer, val.Length);
            }
        }
    }

    /// <summary>
    /// Check how many bytes it will take to write the given string.
    /// </summary>
    /// <remarks>
    /// Size is the UTF-8 byte count as a 'packed unsigned integer' plus the encoded bytes.
    /// </remarks>
    /// <param name="val">Value to get the size for.</param>
    /// <returns>Number of bytes it will take.</returns>
    public static int GetSizeForString(ReadOnlySpan<char> val)
    {
        unsafe
        {
            fixed (char* charPointer = val)
            {
                return GetSizeForString(charPointer, val.Length);
            }
        }
    }

    /// <summary>
    /// Check how many bytes it will take to write the given string.
    /// Make sure the data behind the pointer is pinned before calling this.
    /// </summary>
    /// <remarks>
    /// Size is the UTF-8 byte count as a 'packed unsigned integer' plus the encoded bytes.
    /// </remarks>
    /// <param name="charPointer">Pointer to the first character.</param>
    /// <param name="charCount">How many characters are in the string.</param>
    /// <returns>Number of bytes it will take.</returns>
    public static unsafe int GetSizeForString(char* charPointer, int charCount)
    {
        var charsSize = Uft8.GetByteCount(charPointer, charCount);
        var headerSize = GetSizeForPackedUnsignedInteger((uint)charsSize);
        return headerSize + charsSize;
    }

    public static void WriteString(Stream stream, string? val)
    {
        var size = GetSizeForString(val);
        var buff = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            var span = new Span<byte>(buff, 0, size);
            WriteString(ref span, val);
            stream.Write(buff, 0, size);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buff);
        }
    }

    public static void WriteString(IBufferWriter<byte> span, string? val)
    {
        var size = GetSizeForString(val);
        var spanPointer = span.GetSpan(size);
        WriteString(ref spanPointer, val);
        span.Advance(size);
    }

    /// <summary>
    /// Write a string as utf8.
    /// </summary>
    /// <remarks>
    /// Prefixes the data with a 'packed unsigned integer' telling how many bytes will follow.
    /// Format will match that of a <see cref="System.IO.BinaryWriter"/> that is using utf8 encoding.
    /// </remarks>
    /// <param name="span">Span to write to.</param>
    /// <param name="val">Value to write.</param>
    public static void WriteString(ref Span<byte> span, string? val)
    {
        unsafe
        {
            if (string.IsNullOrWhiteSpace(val))
            {
                WritePackedUnsignedInteger(ref span, 0);
                return;
            }

            fixed (char* charPointer = val)
            {
                WriteString(ref span, charPointer, val.Length);
            }
        }
    }

    public static void WriteString(ref Memory<byte> span, string? val)
    {
        unsafe
        {
            if (string.IsNullOrWhiteSpace(val))
            {
                WritePackedUnsignedInteger(ref span, 0);
                return;
            }

            fixed (char* charPointer = val)
            {
                WriteString(ref span, charPointer, val.Length);
            }
        }
    }

    /// <summary>
    /// Write a string as utf8.
    /// </summary>
    /// <remarks>
    /// Prefixes the data with a 'packed unsigned integer' telling how many bytes will follow.
    /// Format will match that of a <see cref="System.IO.BinaryWriter"/> that is using utf8 encoding.
    /// </remarks>
    /// <param name="span">Span to write to.</param>
    /// <param name="val">Value to write.</param>
    public static void WriteString(ref Span<byte> span, in ReadOnlySpan<char> val)
    {
        unsafe
        {
            fixed (char* charPointer = val)
            {
                WriteString(ref span, charPointer, val.Length);
            }
        }
    }

    public static void WriteString(IBufferWriter<byte> span, in ReadOnlySpan<char> val)
    {
        var size = GetSizeForString(val);
        var spanPointer = span.GetSpan(size);
        WriteString(ref spanPointer, val);
        span.Advance(size);
    }

    public static void WriteString(ref Memory<byte> span, in ReadOnlySpan<char> val)
    {
        unsafe
        {
            fixed (char* charPointer = val)
            {
                WriteString(ref span, charPointer, val.Length);
            }
        }
    }

    /// <summary>
    /// Write a string as utf8.
    /// Make sure the data behind the pointer is pinned before calling this.
    /// </summary>
    /// <remarks>
    /// Prefixes the data with a 'packed unsigned integer' telling how many bytes will follow.
    /// Format will match that of a <see cref="System.IO.BinaryWriter"/> that is using utf8 encoding.
    /// </remarks>
    /// <param name="span">Span to write to.</param>
    /// <param name="charPointer">Pointer to the first character.</param>
    /// <param name="charCount">How many characters are in the string.</param>
    public static unsafe void WriteString(ref Span<byte> span, char* charPointer, int charCount)
    {
        // Write amount of bytes will follow.
        var byteCount = Uft8.GetByteCount(charPointer, charCount);
        WritePackedUnsignedInteger(ref span, (uint)byteCount);
        if (charCount != 0)
        {
            fixed (byte* spanPointer = span)
            {
                // Write chars as utf8.
                var writtenBytes = Uft8.GetBytes(charPointer, charCount, spanPointer, span.Length);
                Debug.Assert(
                    byteCount == writtenBytes,
                    "Written bytes did not match encodings expected size"
                );
            }

            // 'Advance' the span.
            span = span[byteCount..];
        }
    }

    public static unsafe void WriteString(ref Memory<byte> memory, char* charPointer, int charCount)
    {
        // Write amount of bytes will follow.
        var byteCount = Uft8.GetByteCount(charPointer, charCount);
        WritePackedUnsignedInteger(ref memory, (uint)byteCount);
        if (charCount != 0)
        {
            fixed (byte* spanPointer = memory.Span)
            {
                // Write chars as utf8.
                var writtenBytes = Uft8.GetBytes(
                    charPointer,
                    charCount,
                    spanPointer,
                    memory.Length
                );
                Debug.Assert(
                    byteCount == writtenBytes,
                    "Written bytes did not match encodings expected size"
                );
            }

            // 'Advance' the span.
            memory = memory[byteCount..];
        }
    }

    public static unsafe void WriteString(
        IBufferWriter<byte> span,
        char* charPointer,
        int charCount
    )
    {
        var size = GetSizeForString(charPointer, charCount);
        var spanPointer = span.GetSpan(size);
        WriteString(ref spanPointer, charPointer, charCount);
        span.Advance(size);
    }

    public static string ReadString(Stream stream) => ReadString(stream, int.MaxValue);

    /// <summary>Reads a UTF-8 string whose encoded length does not exceed the given limit.</summary>
    public static string ReadString(Stream stream, int maxByteCount)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentOutOfRangeException.ThrowIfNegative(maxByteCount);
        var byteCount = ValidateStringByteCount(ReadPackedUnsignedInteger(stream), maxByteCount);
        if (byteCount == 0)
        {
            return string.Empty;
        }
        if (stream.CanSeek && stream.Length - stream.Position < byteCount)
        {
            throw new EndOfStreamException("The stream does not contain the complete string.");
        }

        // Bound temporary storage even when a non-seekable stream advertises a large string.
        const int chunkSize = 4096;
        var bytes = ArrayPool<byte>.Shared.Rent(Math.Min(byteCount, chunkSize));
        try
        {
            if (byteCount <= chunkSize)
            {
                stream.ReadExactly(bytes, 0, byteCount);
                return Uft8.GetString(bytes, 0, byteCount);
            }

            var chars = ArrayPool<char>.Shared.Rent(Uft8.GetMaxCharCount(chunkSize));
            try
            {
                var decoder = Uft8.GetDecoder();
                var result = new StringBuilder(chunkSize);
                var remaining = byteCount;
                while (remaining > 0)
                {
                    var read = stream.Read(bytes, 0, Math.Min(remaining, chunkSize));
                    if (read == 0)
                    {
                        throw new EndOfStreamException(
                            "The stream does not contain the complete string."
                        );
                    }
                    remaining -= read;
                    var written = decoder.GetChars(bytes, 0, read, chars, 0, remaining == 0);
                    result.Append(chars, 0, written);
                }
                return result.ToString();
            }
            finally
            {
                ArrayPool<char>.Shared.Return(chars);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bytes);
        }
    }

    private static int ValidateStringByteCount(uint size, int maxByteCount)
    {
        if (size > maxByteCount)
        {
            throw new InvalidDataException(
                $"String length {size} exceeds the allowed {maxByteCount} bytes."
            );
        }
        return (int)size;
    }

    /// <summary>
    /// Read a string.
    /// </summary>
    /// <remarks>
    /// Can only be used for strings less then 128 kib as utf8, for bigger strings use a overload
    /// where you pass a 'Span{char}' as the output buffer.
    /// </remarks>
    /// <param name="span">Span to read from.</param>
    /// <returns>Read string.</returns>
    public static string ReadString(ref ReadOnlySpan<byte> span)
    {
        // Read how many bytes will follow.
        var byteCount = ValidateStringByteCount(ReadPackedUnsignedInteger(ref span), int.MaxValue);

        if (byteCount == 0)
        {
            return string.Empty;
        }

        // Check if the span contains the entire string.
        if (span.Length < byteCount)
        {
            throw new ArgumentOutOfRangeException(nameof(span), "Given span is incomplete");
        }

        // Preserve the size limit of the existing span overload.
        if (byteCount >= MaxStackStringBytes)
        {
            throw new ArgumentException(
                "Input contains a string exceeding the supported byte length",
                nameof(span)
            );
        }

        var value = Uft8.GetString(span[..byteCount]);
        span = span[byteCount..];
        return value;
    }

    public static string ReadString(ref ReadOnlyMemory<byte> span)
    {
        var originSpan = span.Span;
        var str = ReadString(ref originSpan);
        var size = span.Span.Length - originSpan.Length;
        span = span[size..];
        return str;
    }

    /// <summary>
    /// Read a string to a given output-buffer.
    /// </summary>
    /// <param name="span">Span to read from.</param>
    /// <param name="chars">Buffer to write to.</param>
    /// <returns>Amount of characters written</returns>
    public static int ReadString(ref ReadOnlySpan<byte> span, Span<char> chars)
    {
        unsafe
        {
            // Read amount of bytes will follow.
            var byteCount = ValidateStringByteCount(
                ReadPackedUnsignedInteger(ref span),
                int.MaxValue
            );

            // Check if input span contains the entire string.
            if (span.Length < byteCount)
            {
                throw new ArgumentOutOfRangeException(nameof(span), "Given span is incomplete");
            }

            // No need to check if the output span has enough space as 'Encoding.GetChars' will
            // already do that for us.

            // Read chars as utf8.
            int charsRead;
            fixed (char* charPointer = chars)
            fixed (byte* bytePointer = span)
            {
                charsRead = Uft8.GetChars(bytePointer, byteCount, charPointer, chars.Length);
            }

            // 'Advance' the span.
            span = span[byteCount..];

            return charsRead;
        }
    }

    public static bool TryReadString(ref SequenceReader<byte> reader, ref string value) =>
        TryReadString(ref reader, ref value, int.MaxValue);

    /// <summary>Reads a complete UTF-8 string without consuming partial input.</summary>
    public static bool TryReadString(
        ref SequenceReader<byte> reader,
        ref string value,
        int maxByteCount
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxByteCount);
        var candidate = reader;
        uint size = 0;
        if (!TryReadPackedUnsignedInteger(ref candidate, ref size))
        {
            return false;
        }
        var byteCount = ValidateStringByteCount(size, maxByteCount);
        if (candidate.Remaining < byteCount)
        {
            return false;
        }

        if (candidate.UnreadSpan.Length >= byteCount)
        {
            value = Uft8.GetString(candidate.UnreadSpan[..byteCount]);
        }
        else
        {
            var buffer = ArrayPool<byte>.Shared.Rent(byteCount);
            try
            {
                var span = buffer.AsSpan(0, byteCount);
                if (!candidate.TryCopyTo(span))
                {
                    return false;
                }
                value = Uft8.GetString(span);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        candidate.Advance(byteCount);
        reader = candidate;
        return true;
    }
}
