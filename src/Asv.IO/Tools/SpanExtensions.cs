using System;
using System.Text;

namespace Asv.IO
{
    public static class SpanExtensions
    {
        public static string GetString(this ReadOnlySpan<byte> span, Encoding encoding)
        {
            return encoding.GetString(span);
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
