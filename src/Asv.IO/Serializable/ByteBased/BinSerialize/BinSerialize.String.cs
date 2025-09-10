using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;

namespace Asv.IO;

public static partial class BinSerialize
{
    /// <summary>
    /// Check how many bytes it will take to write the given string value.
    /// </summary>
    /// <remarks>
    /// Size will be the length of the string as a 'packed unsigned integer' + the amount of
    /// bytes when the characters are utf-8 encoded.
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
    /// Size will be the length of the span as a 'packed unsigned integer' + the amount of
    /// bytes when the characters are utf-8 encoded.
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
    /// Size will be the charCount as a 'packed unsigned integer' + the amount of
    /// bytes when the characters are utf-8 encoded.
    /// </remarks>
    /// <param name="charPointer">Pointer to the first character.</param>
    /// <param name="charCount">How many characters are in the string.</param>
    /// <returns>Number of bytes it will take.</returns>
    public static unsafe int GetSizeForString(char* charPointer, int charCount)
    {
        var headerSize = GetSizeForPackedUnsignedInteger((uint)charCount);
        var charsSize = Uft8.GetByteCount(charPointer, charCount);
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

    public static string ReadString(Stream stream)
    {
        int byteCount = (int)ReadPackedUnsignedInteger(stream);
        if (byteCount == 0)
        {
            return string.Empty;
        }

        var bytes = ArrayPool<byte>.Shared.Rent(byteCount);
        var chars = ArrayPool<char>.Shared.Rent(Uft8.GetMaxCharCount(byteCount));
        try
        {
            stream.ReadExactly(bytes, 0, byteCount);
            int charsWritten = Uft8.GetChars(bytes, 0, byteCount, chars, 0);
            return new string(chars, 0, charsWritten);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bytes);
            ArrayPool<char>.Shared.Return(chars);
        }
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
        var byteCount = (int)ReadPackedUnsignedInteger(ref span);

        if (byteCount == 0)
        {
            return string.Empty;
        }

        // Check if the span contains the entire string.
        if (span.Length < byteCount)
        {
            throw new ArgumentOutOfRangeException(nameof(span), "Given span is incomplete");
        }

        // Sanity check the size before allocating space on the stack.
        if (byteCount >= MaxStackStringBytes)
        {
            throw new ArgumentException(
                "Input contains a string with too many bytes to fit on the stack",
                nameof(span)
            );
        }

        // Decode on the stack to avoid having to allocate a temporary buffer on the heap.
        var maxCharCount = Uft8.GetMaxCharCount(byteCount);

        // var charBuffer = stackalloc char[maxCharCount];
        var charBuffer = ArrayPool<char>.Shared.Rent(maxCharCount); // faster then  stackalloc char[maxCharCount]; (https://stackoverflow.com/questions/55229518/why-allocation-on-arraypool-is-faster-then-allocation-on-stack)

        try
        {
            unsafe
            {
                // Read chars as utf8.
                int actualCharCount;
                fixed (byte* bytePointer = span)
                fixed (char* charPointer = charBuffer)
                {
                    actualCharCount = Uft8.GetChars(
                        bytePointer,
                        byteCount,
                        charPointer,
                        maxCharCount
                    );
                }

                // 'Advance' the span.
                span = span[byteCount..];

                // Allocate the string.
                return new string(charBuffer, startIndex: 0, length: actualCharCount);
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(charBuffer);
        }
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
            var byteCount = (int)ReadPackedUnsignedInteger(ref span);

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

    public static bool TryReadString(ref SequenceReader<byte> reader, ref string value)
    {
        uint size = 0;
        if (TryReadPackedUnsignedInteger(ref reader, ref size, out var consumed) == false)
        {
            return false;
        }
        var buff = ArrayPool<byte>.Shared.Rent((int)size);
        try
        {
            var span = new Span<byte>(buff, 0, (int)size);
            if (reader.TryCopyTo(span) == false)
            {
                reader.Rewind(consumed);
                return false;
            }
            reader.Advance(size);
            value = Uft8.GetString(span);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buff);
        }

        return true;
    }
}
