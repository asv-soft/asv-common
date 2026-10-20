using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Asv.IO.Test;

public class ProtocolEncodingMixinTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(8)]
    public void EncodeToArray_ReturnsOnlyWrittenBytes_AndEncodesOnce(int written)
    {
        var encoder = new TestEncoder(8, written);

        var result = new ExampleMessage3().EncodeToArray(encoder);

        Assert.Equal(ExpectedBytes(written), result);
        Assert.Equal(1, encoder.EncodeCalls);
        Assert.Equal(1, encoder.MeasureCalls);
        Assert.Equal(8, encoder.DestinationLength);
    }

    [Fact]
    public void EncodeTo_OversizedWriter_ConstrainsCapacityAndAdvancesActualLength()
    {
        var encoder = new TestEncoder(8, 3);
        var writer = new ArrayBufferWriter<byte>(64);
        writer.GetSpan().Fill(0xCC);

        var written = new ExampleMessage3().EncodeTo(writer, encoder);

        Assert.Equal(3, written);
        Assert.Equal(new byte[] { 1, 2, 3 }, writer.WrittenSpan.ToArray());
        Assert.Equal(8, encoder.DestinationLength);
        Assert.Equal(1, encoder.EncodeCalls);
        Assert.Equal(1, encoder.MeasureCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EncodeToStream_AppendsOnlyEncodedBytes(bool asynchronous)
    {
        var encoder = new TestEncoder(8, 3);
        using var stream = new MemoryStream();
        stream.WriteByte(0xAA);
        var message = new ExampleMessage3();

        var written = asynchronous
            ? await message.EncodeToStreamAsync(
                stream,
                encoder,
                TestContext.Current.CancellationToken
            )
            : message.EncodeToStream(stream, encoder);

        Assert.Equal(3, written);
        Assert.Equal(new byte[] { 0xAA, 1, 2, 3 }, stream.ToArray());
        Assert.Equal(8, encoder.DestinationLength);
        Assert.Equal(1, encoder.EncodeCalls);
        Assert.Equal(1, encoder.MeasureCalls);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(9)]
    public async Task InvalidEncodedLength_IsRejectedBeforePublishing(int written)
    {
        var message = new ExampleMessage3();
        var writer = new ArrayBufferWriter<byte>();
        using var stream = new MemoryStream();

        Assert.Throws<InvalidOperationException>(() =>
            message.EncodeToArray(new TestEncoder(8, written))
        );
        Assert.Throws<InvalidOperationException>(() =>
            message.EncodeTo(writer, new TestEncoder(8, written))
        );
        Assert.Throws<InvalidOperationException>(() =>
            message.EncodeToStream(stream, new TestEncoder(8, written))
        );
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await message.EncodeToStreamAsync(
                stream,
                new TestEncoder(8, written),
                TestContext.Current.CancellationToken
            )
        );

        Assert.Equal(0, writer.WrittenCount);
        Assert.Equal(0, stream.Length);
    }

    [Fact]
    public void NegativeMaximumSize_IsRejectedWithoutEncoding()
    {
        var encoder = new TestEncoder(-1, 0);

        Assert.Throws<InvalidOperationException>(() =>
            new ExampleMessage3().EncodeToArray(encoder)
        );

        Assert.Equal(0, encoder.EncodeCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EncodeToStream_WriteFailure_PropagatesWithoutReencoding(bool asynchronous)
    {
        var encoder = new TestEncoder(8, 3);
        using var stream = new FailingStream();
        var message = new ExampleMessage3();

        var exception = asynchronous
            ? await Assert.ThrowsAsync<IOException>(async () =>
                await message.EncodeToStreamAsync(
                    stream,
                    encoder,
                    TestContext.Current.CancellationToken
                )
            )
            : Assert.Throws<IOException>(() => message.EncodeToStream(stream, encoder));

        Assert.Same(stream.Failure, exception);
        Assert.Equal(1, encoder.EncodeCalls);
        Assert.Equal(1, stream.WriteCalls);
    }

    [Fact]
    public async Task EncodeToStreamAsync_CancelledBeforeEncoding_DoesNotAdvanceEncoder()
    {
        var encoder = new TestEncoder(8, 3);
        using var stream = new MemoryStream();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new ExampleMessage3().EncodeToStreamAsync(
                stream,
                encoder,
                new CancellationToken(true)
            )
        );

        Assert.Equal(0, encoder.EncodeCalls);
        Assert.Equal(0, encoder.MeasureCalls);
        Assert.Equal(0, stream.Length);
    }

    private static byte[] ExpectedBytes(int count)
    {
        var bytes = new byte[count];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(i + 1);
        }

        return bytes;
    }

    private sealed class TestEncoder(int maximum, int written) : IProtocolEncoder
    {
        public int EncodeCalls { get; private set; }
        public int MeasureCalls { get; private set; }
        public int DestinationLength { get; private set; }

        public int GetMaxByteSize(IProtocolMessage message)
        {
            MeasureCalls++;
            return maximum;
        }

        public int Encode(IProtocolMessage message, Span<byte> destination)
        {
            EncodeCalls++;
            DestinationLength = destination.Length;
            destination.Fill(0xCC);
            if (written >= 0 && written <= destination.Length)
            {
                ExpectedBytes(written).CopyTo(destination);
            }

            return written;
        }
    }

    private sealed class FailingStream : MemoryStream
    {
        public IOException Failure { get; } = new("Write failed.");
        public int WriteCalls { get; private set; }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteCalls++;
            throw Failure;
        }

        public override ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default
        )
        {
            WriteCalls++;
            return ValueTask.FromException(Failure);
        }
    }
}
