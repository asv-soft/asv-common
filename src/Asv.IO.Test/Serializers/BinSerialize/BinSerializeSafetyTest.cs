using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Asv.IO.Test;

public class BinSerializeSafetyTest
{
    [Theory]
    [InlineData("Int16", 2)]
    [InlineData("UInt16", 2)]
    [InlineData("Int32", 4)]
    [InlineData("UInt32", 4)]
    [InlineData("Int64", 8)]
    [InlineData("UInt64", 8)]
    [InlineData("Struct", 8)]
    public void Reserve_ShortSpan_DoesNotModifyBackingBuffer(string kind, int size)
    {
        for (var length = 0; length < size; length++)
        {
            var backing = Enumerable.Repeat((byte)0xAA, 32).ToArray();
            Assert.Throws<ArgumentOutOfRangeException>(() => ReserveSlice(kind, backing, length));
            Assert.All(backing, value => Assert.Equal(0xAA, value));
        }
    }

    [Theory]
    [InlineData("Int16", 2)]
    [InlineData("UInt16", 2)]
    [InlineData("Int32", 4)]
    [InlineData("UInt32", 4)]
    [InlineData("Int64", 8)]
    [InlineData("UInt64", 8)]
    [InlineData("Struct", 8)]
    public void Reserve_ExactSpan_ClearsOnlyReservedBytes(string kind, int size)
    {
        var backing = Enumerable.Repeat((byte)0xAA, 32).ToArray();
        Assert.Equal(0, ReserveSlice(kind, backing, size));
        var end = 5 + size;
        Assert.All(backing[..5], value => Assert.Equal(0xAA, value));
        Assert.All(backing[5..end], value => Assert.Equal(0, value));
        Assert.All(backing[end..], value => Assert.Equal(0xAA, value));
    }

    [Fact]
    public void Reserve_ValidSpan_ReturnedReferenceStillWritesIntoReservedRegion()
    {
        var backing = new byte[8];
        var span = backing.AsSpan();
        ref var value = ref BinSerialize.ReserveInt(ref span);
        value = 123456;
        ReadOnlySpan<byte> input = backing;
        Assert.Equal(123456, BinSerialize.ReadInt(ref input));
        Assert.Equal(4, span.Length);
    }

    [Theory]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(8191)]
    [InlineData(8192)]
    public void String_MultibyteLengthBoundary_MatchesBinaryWriterAndPreservesNextField(int length)
    {
        var value = new string('я', length);
        using var expectedStream = new MemoryStream();
        using (var writer = new BinaryWriter(expectedStream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(value);
            writer.Write((byte)0xA5);
        }
        var expected = expectedStream.ToArray();
        Assert.Equal(expected.Length - 1, BinSerialize.GetSizeForString(value));
        Assert.Equal(expected.Length - 1, BinSerialize.GetSizeForString(value.AsSpan()));

        var exact = new byte[expected.Length - 1];
        var span = exact.AsSpan();
        BinSerialize.WriteString(ref span, value);
        Assert.True(span.IsEmpty);
        Assert.Equal(expected[..^1], exact);

        var destination = new ArrayBufferWriter<byte>();
        BinSerialize.WriteString(destination, value);
        BinSerialize.WriteByte(destination, 0xA5);
        Assert.Equal(expected, destination.WrittenSpan.ToArray());
        var read = destination.WrittenSpan;
        Assert.Equal(value, BinSerialize.ReadString(ref read));
        Assert.Equal(0xA5, BinSerialize.ReadByte(ref read));
        Assert.True(read.IsEmpty);
    }

    [Fact]
    public void TryReadString_IncompletePayload_DoesNotRentOrConsumeInput()
    {
        using var events = new PoolRentListener();
        var reader = new SequenceReader<byte>(
            new ReadOnlySequence<byte>(new byte[] { 0x80, 0x80, 0x04 })
        );
        var value = "previous";
        events.Start();
        var result = BinSerialize.TryReadString(ref reader, ref value);
        events.Stop();

        Assert.False(result);
        Assert.Equal(0, events.Rents);
        Assert.Equal(0, reader.Consumed);
        Assert.Equal("previous", value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TryReadString_CompletePayload_ConsumesOnlyString(bool segmented)
    {
        var bytes = new byte[] { 2, 0xD1, 0x8F, 0xA5 };
        var sequence = segmented
            ? TestSequence.Create(bytes.AsMemory(0, 2), bytes.AsMemory(2))
            : new ReadOnlySequence<byte>(bytes);
        var reader = new SequenceReader<byte>(sequence);
        var value = string.Empty;

        Assert.True(BinSerialize.TryReadString(ref reader, ref value, 2));
        Assert.Equal("я", value);
        Assert.Equal(3, reader.Consumed);
        Assert.True(reader.TryRead(out var next));
        Assert.Equal(0xA5, next);
    }

    [Fact]
    public void String_LimitOverloads_RejectOversizedLengthBeforePayloadRead()
    {
        using var stream = new FragmentedStream([3, 1, 2, 3], 1);
        Assert.Throws<InvalidDataException>(() => BinSerialize.ReadString(stream, 2));
        Assert.Equal(1, stream.PositionRead);

        var sequence = new ReadOnlySequence<byte>(new byte[] { 3, 1, 2, 3 });
        Assert.Throws<InvalidDataException>(() => ReadLimitedSequence(sequence));
    }

    [Fact]
    public void ReadString_NonSeekableTruncatedLargeValue_UsesBoundedReadRequests()
    {
        using var stream = new FragmentedStream([0x80, 0x80, 0x04], int.MaxValue);
        Assert.Throws<EndOfStreamException>(() => BinSerialize.ReadString(stream));
        Assert.InRange(stream.MaxReadRequest, 1, 4096);
        Assert.Equal(3, stream.PositionRead);
    }

    [Theory]
    [InlineData(3000)]
    [InlineData(70000)]
    public void ReadString_NonSeekableLargeUtf8_RoundTripsAcrossCharacterBoundaries(int length)
    {
        var value = new string('я', length) + "😀" + new string('a', 2000);
        using var encoded = new MemoryStream();
        using (var writer = new BinaryWriter(encoded, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(value);
            writer.Write((byte)0xA5);
        }
        using var stream = new FragmentedStream(encoded.ToArray(), 3);
        Assert.Equal(value, BinSerialize.ReadString(stream));
        Assert.Equal(0xA5, stream.ReadByte());
        Assert.Equal(-1, stream.ReadByte());
        Assert.InRange(stream.MaxReadRequest, 1, 4096);
    }

    [Fact]
    public void ReadString_SeekableTruncatedValue_DoesNotRent()
    {
        using var stream = new MemoryStream([0x80, 0x80, 0x04]);
        using var events = new PoolRentListener();
        events.Start();
        Assert.Throws<EndOfStreamException>(() => BinSerialize.ReadString(stream));
        events.Stop();
        Assert.Equal(0, events.Rents);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TryReadHalf_PreservesValueAndFollowingData(bool segmented)
    {
        var bytes = new byte[3];
        var write = bytes.AsSpan();
        BinSerialize.WriteHalf(ref write, (Half)1.5);
        write[0] = 0xA5;
        var sequence = segmented
            ? TestSequence.Create(bytes.AsMemory(0, 1), bytes.AsMemory(1))
            : new ReadOnlySequence<byte>(bytes);
        var reader = new SequenceReader<byte>(sequence);
        var value = (Half)0;

        Assert.True(BinSerialize.TryReadHalf(ref reader, ref value));
        Assert.Equal((Half)1.5, value);
        Assert.True(reader.TryRead(out var next));
        Assert.Equal(0xA5, next);

        reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(new byte[] { 0 }));
        Assert.False(BinSerialize.TryReadHalf(ref reader, ref value));
        Assert.Equal(0, reader.Consumed);
        Assert.Equal((Half)1.5, value);
    }

    [Fact]
    public void SpanGetString_CompleteAndIncompleteUtf8_MatchesEncoding()
    {
        foreach (
            var bytes in new[] { Encoding.UTF8.GetBytes("sample я😀"), new byte[] { 0xE2, 0x82 } }
        )
        {
            Assert.Equal(
                Encoding.UTF8.GetString(bytes),
                SpanExtensions.GetString(bytes, Encoding.UTF8)
            );
        }
    }

    private static void ReadLimitedSequence(ReadOnlySequence<byte> sequence)
    {
        var reader = new SequenceReader<byte>(sequence);
        var value = string.Empty;
        BinSerialize.TryReadString(ref reader, ref value, 2);
    }

    private static int ReserveSlice(string kind, byte[] backing, int length)
    {
        var span = backing.AsSpan(5, length);
        switch (kind)
        {
            case "Int16":
                BinSerialize.ReserveShort(ref span);
                break;
            case "UInt16":
                BinSerialize.ReserveUShort(ref span);
                break;
            case "Int32":
                BinSerialize.ReserveInt(ref span);
                break;
            case "UInt32":
                BinSerialize.ReserveUInt(ref span);
                break;
            case "Int64":
                BinSerialize.ReserveLong(ref span);
                break;
            case "UInt64":
                BinSerialize.ReserveULong(ref span);
                break;
            case "Struct":
                BinSerialize.ReserveStruct<long>(ref span);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
        return span.Length;
    }

    private sealed class FragmentedStream(byte[] bytes, int fragmentSize) : Stream
    {
        public int PositionRead { get; private set; }
        public int MaxReadRequest { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            MaxReadRequest = Math.Max(MaxReadRequest, count);
            var read = Math.Min(Math.Min(count, fragmentSize), bytes.Length - PositionRead);
            bytes.AsSpan(PositionRead, read).CopyTo(buffer.AsSpan(offset));
            PositionRead += read;
            return read;
        }

        public override int ReadByte() => PositionRead < bytes.Length ? bytes[PositionRead++] : -1;

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();
    }

    private sealed class PoolRentListener : System.Diagnostics.Tracing.EventListener
    {
        private int _thread;
        public int Rents { get; private set; }

        public void Start()
        {
            Rents = 0;
            _thread = Environment.CurrentManagedThreadId;
        }

        public void Stop() => _thread = 0;

        protected override void OnEventSourceCreated(System.Diagnostics.Tracing.EventSource source)
        {
            if (source.Name == "System.Buffers.ArrayPoolEventSource")
            {
                EnableEvents(source, System.Diagnostics.Tracing.EventLevel.Verbose);
            }
        }

        protected override void OnEventWritten(
            System.Diagnostics.Tracing.EventWrittenEventArgs eventData
        )
        {
            if (
                _thread == Environment.CurrentManagedThreadId
                && eventData.EventName == "BufferRented"
            )
            {
                Rents++;
            }
        }
    }
}

internal sealed class TestSequence : ReadOnlySequenceSegment<byte>
{
    private TestSequence(ReadOnlyMemory<byte> memory) => Memory = memory;

    public static ReadOnlySequence<byte> Create(
        ReadOnlyMemory<byte> first,
        ReadOnlyMemory<byte> second
    )
    {
        var head = new TestSequence(first);
        var tail = new TestSequence(second) { RunningIndex = first.Length };
        head.Next = tail;
        return new ReadOnlySequence<byte>(head, 0, tail, second.Length);
    }
}
