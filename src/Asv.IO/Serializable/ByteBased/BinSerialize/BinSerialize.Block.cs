using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;

namespace Asv.IO;

public partial class BinSerialize
{
    #region ReadBlock

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadBlock(Stream stream, Span<byte> output)
    {
        stream.ReadExactly(output);
    }

    /// <summary>
    /// Read a continuous block of bytes as a new byte-array.
    /// </summary>
    /// <remarks>
    /// Will consume '<paramref name="byteCount"/>' amount of bytes.
    /// </remarks>
    /// <param name="span">Span to read from.</param>
    /// <param name="byteCount">Amount of bytes to read.</param>
    /// <returns>New byte-array containing the read bytes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] ReadBlock(ref ReadOnlySpan<byte> span, int byteCount)
    {
        var result = new byte[byteCount];
        ReadBlock(ref span, result);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadBlock(ref ReadOnlySpan<byte> span, Span<byte> output)
    {
        span[..output.Length].CopyTo(output);

        // 'Advance' the span.
        span = span[output.Length..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadBlock(ReadOnlySpan<byte> span, Span<byte> output)
    {
        span[..output.Length].CopyTo(output);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadBlock(ReadOnlyMemory<byte> span, Span<byte> output)
    {
        span[..output.Length].Span.CopyTo(output);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadBlock(ref ReadOnlyMemory<byte> span, Span<byte> output)
    {
        span[..output.Length].Span.CopyTo(output);

        // 'Advance' the span.
        span = span[output.Length..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadBlock(ref SequenceReader<byte> reader, Span<byte> output)
    {
        if (reader.TryCopyTo(output) == false)
        {
            return false;
        }

        reader.Advance(output.Length);
        return true;
    }

    #endregion

    #region WriteBlock

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBlock(Stream stream, ReadOnlySpan<byte> val)
    {
        stream.Write(val);
    }

    /// <summary>
    /// Write a continuous block of bytes.
    /// </summary>
    /// <remarks>
    /// Will consume as many bytes as are in the given block.
    /// </remarks>
    /// <param name="span">Span to write to.</param>
    /// <param name="val">Block of bytes to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBlock(ref Span<byte> span, ReadOnlySpan<byte> val)
    {
        val.CopyTo(span);

        // 'Advance' the span.
        span = span[val.Length..];
    }

    /// <summary>
    /// Write a continuous block of bytes.
    /// </summary>
    /// <remarks>
    /// Will consume as many bytes as are in the given block.
    /// </remarks>
    /// <param name="span">Span to write to.</param>
    /// <param name="val">Block of bytes to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBlock(Span<byte> span, ReadOnlySpan<byte> val)
    {
        val.CopyTo(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBlock(IBufferWriter<byte> wrt, ReadOnlySpan<byte> val)
    {
        var span = wrt.GetSpan(val.Length);
        WriteBlock(ref span, val);
        wrt.Advance(val.Length);
    }

    #endregion
}
