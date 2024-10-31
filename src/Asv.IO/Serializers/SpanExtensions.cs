using System;
using System.Buffers;
using System.Text;

namespace Asv.IO
{
    public static class SpanExtensions
    {
        public static string GetString(this ReadOnlySpan<byte> span, Encoding encoding)
        {
            unsafe
            {
                var maxCharCount = encoding.GetMaxCharCount(span.Length);
                var charBuffer = ArrayPool<char>.Shared.Rent(maxCharCount); // faster then  stackalloc char[maxCharCount]; (https://stackoverflow.com/questions/55229518/why-allocation-on-arraypool-is-faster-then-allocation-on-stack)
                try
                {
                    int actualCharCount;
                    fixed (byte* bytePointer = span)
                    fixed (char* charPointer = charBuffer)
                    {
                        actualCharCount = encoding
                            .GetDecoder()
                            .GetChars(
                                bytePointer,
                                span.Length,
                                charPointer,
                                maxCharCount,
                                flush: false
                            );
                    }

                    return new string(charBuffer, startIndex: 0, length: actualCharCount);
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(charBuffer);
                }
            }
        }

        public static void CopyTo(this string source, ref Span<byte> span, Encoding encoding)
        {
            unsafe
            {
                fixed (char* charPointer = source)
                {
                    CopyTo(ref span, charPointer, source.Length, encoding);
                }
            }
        }

        public static void CopyTo(
            this ReadOnlySpan<char> source,
            ref Span<byte> span,
            Encoding encoding
        )
        {
            unsafe
            {
                fixed (char* charPointer = source)
                {
                    CopyTo(ref span, charPointer, source.Length, encoding);
                }
            }
        }

        public static unsafe void CopyTo(
            ref Span<byte> span,
            char* charPointer,
            int charCount,
            Encoding encoding
        )
        {
            fixed (byte* spanPointer = span)
            {
                var writtenBytes = encoding.GetBytes(
                    charPointer,
                    charCount,
                    spanPointer,
                    span.Length
                );
                span = span.Slice(writtenBytes);
            }
        }

        public static int GetByteCount(this Encoding encoding, ReadOnlySpan<char> source)
        {
            unsafe
            {
                fixed (char* charPointer = source)
                {
                    return Encoding.ASCII.GetByteCount(charPointer, source.Length);
                }
            }
        }
    }
}
