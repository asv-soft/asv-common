using System;
using System.Buffers;
using System.IO;
using Xunit;

namespace Asv.IO.Test;

public partial class BinSerializeTest
{
    [Theory]
    [InlineData(ushort.MinValue)]
    [InlineData(ushort.MaxValue)]
    [InlineData(1337)]
    public void UShortCanBeSerialized(ushort val)
    {
        var buffer = new byte[2];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.WriteUShort(ref writeSpan, val);

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(val, BinSerialize.ReadUShort(ref readSpan));
    }

    [Fact]
    public void UShortCanBeReserved()
    {
        var buffer = new byte[2];
        var writeSpan = new Span<byte>(buffer);

        ref ushort reserved = ref BinSerialize.ReserveUShort(ref writeSpan);
        reserved = 1337;

        var readSpan = new ReadOnlySpan<byte>(buffer);
        Assert.Equal(1337, BinSerialize.ReadUShort(ref readSpan));
    }

    [Fact]
    public void ReserveUShort_ShouldReturnRef_ZeroInit_AndAdvanceSpan()
    {
        var buffer = new byte[4] { 0xFF, 0xFF, 0xAA, 0xBB }; // prefill to catch zeroing
        Span<byte> span = buffer;

        ref ushort reserved = ref BinSerialize.ReserveUShort(ref span);

        // Should be zero-initialized
        Assert.Equal((ushort)0, reserved);

        // Span advanced by 2 bytes
        Assert.Equal(2, span.Length);

        // Underlying buffer first two bytes must be zeroed (little-endian view irrelevant here)
        Assert.Equal(0x00, buffer[0]);
        Assert.Equal(0x00, buffer[1]);

        // Write through the returned ref and verify buffer reflects it (LE)
        reserved = 0x1234;
        Assert.Equal(0x34, buffer[0]);
        Assert.Equal(0x12, buffer[1]);
    }

    [Fact]
    public void ReadUShort_Stream_ByRef_ShouldReadLittleEndian_AndConsume2Bytes()
    {
        // 0x3412 in LE = bytes: 0x12, 0x34
        using var ms = new MemoryStream([0x12, 0x34, 0x99]);
        ushort val = 0;
        BinSerialize.ReadUShort(ms, ref val);
        Assert.Equal((ushort)0x3412, val);
        Assert.Equal(2, ms.Position); // consumed exactly 2 bytes
    }

    [Fact]
    public void ReadUShort_Stream_Return_ShouldReadLittleEndian_AndConsume2Bytes()
    {
        using var ms = new MemoryStream([0xAB, 0xCD, 0xEF]);
        var val = BinSerialize.ReadUShort(ms);
        Assert.Equal((ushort)0xCDAB, val);
        Assert.Equal(2, ms.Position);
    }

    [Fact]
    public void ReadUShort_Stream_ShouldThrow_OnUnexpectedEof()
    {
        using var ms = new MemoryStream([0x01]); // only 1 byte
        Assert.Throws<EndOfStreamException>(() =>
        {
            var _ = BinSerialize.ReadUShort(ms);
        });
    }

    [Fact]
    public void ReadUShort_ReadOnlySpan_ByRefReturn_ShouldAdvanceSpan_AndReturnValue()
    {
        ReadOnlySpan<byte> span = [0x78, 0x56, 0xAA, 0xBB];
        var copy = span; // to track advance independently

        var value = BinSerialize.ReadUShort(ref copy);
        Assert.Equal((ushort)0x5678, value);
        Assert.Equal(2, span.Length - copy.Length); // advanced by 2
    }

    [Fact]
    public void ReadUShort_ReadOnlySpan_OutParam_ShouldAdvanceSpan_AndSetValue()
    {
        ReadOnlySpan<byte> span = [0x34, 0x12, 0x9A, 0xBC];
        var copy = span;

        ushort v = 0;
        BinSerialize.ReadUShort(ref copy, ref v);
        Assert.Equal((ushort)0x1234, v);
        Assert.Equal(2, span.Length - copy.Length); // advanced by 2
    }

    [Fact]
    public void ReadUShort_ReadOnlySpan_NoAdvance_Overload_ShouldNotAdvance()
    {
        ReadOnlySpan<byte> span = [0x01, 0x02, 0xFF, 0xEE];
        var before = span;

        ushort v = 0;
        BinSerialize.ReadUShort(span, ref v);
        Assert.Equal((ushort)0x0201, v);

        // No advance expected
        Assert.Equal(before.Length, span.Length);
        Assert.True(before.SequenceEqual(span));
    }

    [Fact]
    public void ReadUShort_ReadOnlyMemory_ByRef_ShouldAdvanceMemory()
    {
        ReadOnlyMemory<byte> mem = new byte[] { 0xEF, 0xBE, 0xAD, 0xDE };
        var copy = mem;

        ushort v = 0;
        BinSerialize.ReadUShort(ref copy, ref v);
        Assert.Equal((ushort)0xBEEF, v);
        Assert.Equal(2, mem.Length - copy.Length);
    }

    [Fact]
    public void ReadUShort_ReadOnlyMemory_NoAdvance_ShouldNotAdvance()
    {
        ReadOnlyMemory<byte> mem = new byte[] { 0xAA, 0x55, 0x00, 0x01 };
        var copy = mem;

        ushort v = 0;
        BinSerialize.ReadUShort(copy, ref v);
        Assert.Equal((ushort)0x55AA, v);

        // No advance
        Assert.Equal(mem.Length, copy.Length);
        Assert.True(mem.Span.SequenceEqual(copy.Span));
    }

    private sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory) => Memory = memory;

        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var seg = new Segment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = seg;
            return seg;
        }
    }

    private static ReadOnlySequence<byte> MultiSegment(params byte[][] chunks)
    {
        if (chunks.Length == 0)
        {
            return ReadOnlySequence<byte>.Empty;
        }

        var first = new Segment(chunks[0]);
        var last = first;
        for (int i = 1; i < chunks.Length; i++)
        {
            last = last.Append(chunks[i]);
        }

        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    [Fact]
    public void TryReadUShort_ShouldReturnFalse_AndNotAdvance_WhenInsufficientData()
    {
        var seq = new ReadOnlySequence<byte>([0x01]); // single byte only
        var reader = new SequenceReader<byte>(seq);
        ushort v = 42; // sentinel

        var ok = BinSerialize.TryReadUShort(ref reader, ref v);

        Assert.False(ok);
        Assert.Equal(1, reader.Remaining);
        Assert.Equal((ushort)42, v); // unchanged
    }

    [Fact]
    public void TryReadUShort_ShouldReadAndAdvance_WhenSingleSegmentFastPath()
    {
        var seq = new ReadOnlySequence<byte>([0xFE, 0xCA, 0x00]);
        var reader = new SequenceReader<byte>(seq);
        ushort v = 0;

        var ok = BinSerialize.TryReadUShort(ref reader, ref v);

        Assert.True(ok);
        Assert.Equal((ushort)0xCAFE, v);
        Assert.Equal(1, reader.Remaining); // 3 - 2 = 1
    }

    [Fact]
    public void TryReadUShort_ShouldReadAndAdvance_WhenMultiSegmentFallback()
    {
        // Put the 2 bytes across the segment boundary to force fallback path
        var seq = MultiSegment(
            [0xAB], // first segment has 1 byte
            [0xCD, 0xEE]
        );
        var reader = new SequenceReader<byte>(seq);
        ushort v = 0;

        var ok = BinSerialize.TryReadUShort(ref reader, ref v);

        Assert.True(ok);
        Assert.Equal((ushort)0xCDAB, v);
        Assert.Equal(1, reader.Remaining); // 3 total - 2 consumed = 1
    }

    [Fact]
    public void WriteUShort_Stream_ShouldWriteLittleEndian_AndNotChangePositionOnReadback()
    {
        using var ms = new MemoryStream();
        BinSerialize.WriteUShort(ms, 0xBEEF);

        var bytes = ms.ToArray();
        Assert.Equal(new byte[] { 0xEF, 0xBE }, bytes);
    }

    [Fact]
    public void WriteUShort_SpanByRef_ShouldWriteLittleEndian_AndAdvanceSpan()
    {
        var buffer = new byte[4];
        Span<byte> span = buffer;
        BinSerialize.WriteUShort(ref span, 0x1234);

        // advanced by 2
        Assert.Equal(2, span.Length); // 4 -> 2

        // LE layout
        Assert.Equal(0x34, buffer[0]);
        Assert.Equal(0x12, buffer[1]);

        // Write another value to verify further advance correctness
        BinSerialize.WriteUShort(ref span, 0xABCD);
        Assert.Equal(0, span.Length);
        Assert.Equal(0xCD, buffer[2]);
        Assert.Equal(0xAB, buffer[3]);
    }

    [Fact]
    public void WriteUShort_Span_NoAdvance_ShouldNotAdvanceButWriteLittleEndian()
    {
        var buffer = new byte[2];
        Span<byte> span = buffer;
        BinSerialize.WriteUShort(span, 0xA1B2);

        // No advance expected
        Assert.Equal(2, span.Length);
        Assert.Equal(0xB2, buffer[0]);
        Assert.Equal(0xA1, buffer[1]);
    }

    private sealed class CollectingBufferWriter : IBufferWriter<byte>
    {
        private byte[] _buffer = [];
        private int _written;

        public void Advance(int count) => _written += count;

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (_buffer.Length - _written < sizeHint)
            {
                Array.Resize(ref _buffer, _written + Math.Max(sizeHint, 16));
            }

            return _buffer.AsMemory(_written);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            if (_buffer.Length - _written < sizeHint)
            {
                Array.Resize(ref _buffer, _written + Math.Max(sizeHint, 16));
            }

            return _buffer.AsSpan(_written);
        }

        public byte[] Written => _buffer.AsSpan(0, _written).ToArray();
    }

    [Fact]
    public void WriteUShort_IBufferWriter_ShouldWriteLittleEndian_AndAdvanceWriter()
    {
        var writer = new CollectingBufferWriter();
        BinSerialize.WriteUShort(writer, 0x7788);

        var outBytes = writer.Written;
        Assert.Equal(new byte[] { 0x88, 0x77 }, outBytes);
    }
}
