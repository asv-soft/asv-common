using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Asv.IO.Test;

public class StreamBitWriterDisposalTest
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, true, true)]
    public async Task Dispose_FlushOrWriteFailure_ClosesOwnedStreamExactlyOnce(
        bool asynchronous,
        bool writeFailure,
        bool leaveOpen
    )
    {
        var stream = new FailingStream(writeFailure);
        var writer = new StreamBitWriter(stream, leaveOpen);
        writer.WriteBit(1);

        if (asynchronous)
        {
            await Assert.ThrowsAsync<IOException>(() => writer.DisposeAsync().AsTask());
        }
        else
        {
            Assert.Throws<IOException>(() => writer.Dispose());
        }
        Assert.Equal(leaveOpen ? 0 : 1, stream.DisposeCount);
        Assert.Equal(writeFailure ? 0 : 1, stream.FlushCount);

        writer.Dispose();
        await writer.DisposeAsync();
        Assert.Equal(leaveOpen ? 0 : 1, stream.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => writer.WriteBit(0));
        if (leaveOpen)
        {
            stream.Dispose();
        }
    }

    private sealed class FailingStream(bool failWrite) : MemoryStream
    {
        public int DisposeCount { get; private set; }
        public int FlushCount { get; private set; }

        public override void WriteByte(byte value)
        {
            if (failWrite)
            {
                throw new IOException("Write failed.");
            }
            base.WriteByte(value);
        }

        public override void Flush()
        {
            FlushCount++;
            throw new IOException("Flush failed.");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeCount++;
            }
            base.Dispose(disposing);
        }

        public override ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
