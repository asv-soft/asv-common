using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using R3;
using Xunit;

namespace Asv.IO.Test;

public class DataStreamHelperTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Send_LegacyOverloads_SendActualLength_AndSerializeOnce(bool withCount)
    {
        using var stream = new DeferredStream();
        var message = new VariableMessage();

        var pending = withCount
            ? stream.Send(message, out _, TestContext.Current.CancellationToken)
            : stream.Send(message, TestContext.Current.CancellationToken);

        Assert.Equal(1, message.SerializeCalls);
        Assert.Equal(3, stream.Count);
        Assert.Equal(new byte[] { 1, 2, 3 }, stream.GetPendingBytes());
        Assert.False(pending.IsCompleted);
        stream.Complete(true);
        Assert.True(await pending);
    }

    [Fact]
    public async Task Send_OutCount_IsAvailableBeforeSendCompletes()
    {
        using var stream = new DeferredStream();

        var pending = stream.Send(
            new VariableMessage(),
            out var written,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(3, written);
        Assert.False(pending.IsCompleted);
        stream.Complete(false);
        Assert.False(await pending);
    }

    [Fact]
    public async Task Send_LegacyDefaultCancellationTokenUsesSerializer()
    {
        using var stream = new DeferredStream();
        var message = new VariableMessage();
        IProtocolMessage protocolMessage = message;

        // The default literal intentionally checks source compatibility of the legacy overload.
#pragma warning disable xUnit1051
        var pending = stream.Send(protocolMessage, default);
#pragma warning restore xUnit1051

        Assert.Equal(1, message.SerializeCalls);
        Assert.Equal(new byte[] { 1, 2, 3 }, stream.GetPendingBytes());
        Assert.Equal(CancellationToken.None, stream.Cancellation);
        stream.Complete(true);
        Assert.True(await pending);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Send_PooledBuffer_IsRetainedUntilDeferredSendCompletes(bool explicitEncoder)
    {
        using var stream = new DeferredStream();
        var pool = new TrackingPool();
        var encoder = new TestEncoder();
        var message = new VariableMessage();

        var pending = explicitEncoder
            ? DataStreamHelper.Send(
                stream,
                message,
                encoder,
                pool,
                TestContext.Current.CancellationToken
            )
            : DataStreamHelper.Send(
                stream,
                message,
                out _,
                pool,
                TestContext.Current.CancellationToken
            );

        Assert.Equal(0, pool.Returns);
        Assert.False(pending.IsCompleted);
        Assert.Same(pool.Buffer, stream.Buffer);
        Assert.Equal(
            explicitEncoder ? new byte[] { 7, 8 } : new byte[] { 1, 2, 3 },
            stream.GetPendingBytes()
        );
        stream.Complete(true);
        Assert.True(await pending);
        Assert.Equal(1, pool.Returns);
        Assert.Equal(explicitEncoder ? 1 : 0, encoder.EncodeCalls);
        Assert.Equal(explicitEncoder ? 0 : 1, message.SerializeCalls);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Send_FailureOrCancellation_ReturnsBufferAfterCompletion(
        bool explicitEncoder,
        bool cancelled
    )
    {
        using var stream = new DeferredStream();
        var pool = new TrackingPool();
        var message = new VariableMessage();
        var pending = explicitEncoder
            ? DataStreamHelper.Send(
                stream,
                message,
                new TestEncoder(),
                pool,
                TestContext.Current.CancellationToken
            )
            : DataStreamHelper.Send(
                stream,
                message,
                out _,
                pool,
                TestContext.Current.CancellationToken
            );

        Assert.Equal(0, pool.Returns);
        if (cancelled)
        {
            stream.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        }
        else
        {
            var failure = new IOException("Send failed.");
            stream.Fail(failure);
            Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => pending));
        }

        Assert.Equal(1, pool.Returns);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Send_SynchronousTransportFailure_ReturnsBufferAndThrowsSynchronously(
        bool explicitEncoder
    )
    {
        using var stream = new DeferredStream
        {
            SynchronousFailure = new IOException("Send failed."),
        };
        var pool = new TrackingPool();
        var message = new VariableMessage();

        var exception = Assert.Throws<IOException>(() =>
        {
            if (explicitEncoder)
            {
                DataStreamHelper.Send(
                    stream,
                    message,
                    new TestEncoder(),
                    pool,
                    TestContext.Current.CancellationToken
                );
            }
            else
            {
                DataStreamHelper.Send(
                    stream,
                    message,
                    out _,
                    pool,
                    TestContext.Current.CancellationToken
                );
            }
        });

        Assert.Same(stream.SynchronousFailure, exception);
        Assert.Equal(1, pool.Returns);
    }

    [Fact]
    public async Task Send_ExplicitEncoder_SendsActualLengthAndForwardsCancellationToken()
    {
        using var stream = new DeferredStream();
        using var cancellation = new CancellationTokenSource();
        var message = new VariableMessage();
        var encoder = new TestEncoder();

        var pending = stream.Send(message, encoder, cancellation.Token);

        Assert.Equal(new byte[] { 7, 8 }, stream.GetPendingBytes());
        Assert.Equal(8, encoder.DestinationLength);
        Assert.Equal(cancellation.Token, stream.Cancellation);
        Assert.Equal(1, encoder.EncodeCalls);
        Assert.Equal(0, message.SerializeCalls);
        stream.Complete(true);
        Assert.True(await pending);
    }

    [Fact]
    public void Send_ExplicitEncoder_CancelledBeforeEncoding_DoesNotRentOrEncode()
    {
        using var stream = new DeferredStream();
        var encoder = new TestEncoder();
        var pool = new TrackingPool();

        Assert.ThrowsAny<OperationCanceledException>(() =>
        {
            _ = DataStreamHelper.Send(
                stream,
                new VariableMessage(),
                encoder,
                pool,
                new CancellationToken(true)
            );
        });

        Assert.Equal(0, encoder.EncodeCalls);
        Assert.Equal(0, pool.Rents);
        Assert.Equal(0, stream.SendCalls);
    }

    [Fact]
    public void Send_NullStream_DoesNotRentOrEncode()
    {
        var encoder = new TestEncoder();
        var pool = new TrackingPool();

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = DataStreamHelper.Send(
                null!,
                new VariableMessage(),
                encoder,
                pool,
                TestContext.Current.CancellationToken
            );
        });

        Assert.Equal(0, encoder.EncodeCalls);
        Assert.Equal(0, pool.Rents);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Send_SerializationFailure_ReturnsBufferWithoutSending(bool explicitEncoder)
    {
        using var stream = new DeferredStream();
        var pool = new TrackingPool();
        var failure = new InvalidOperationException("Encoding failed.");
        var message = new VariableMessage { Failure = failure };
        var encoder = new TestEncoder { Failure = failure };

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            if (explicitEncoder)
            {
                DataStreamHelper.Send(
                    stream,
                    message,
                    encoder,
                    pool,
                    TestContext.Current.CancellationToken
                );
            }
            else
            {
                DataStreamHelper.Send(
                    stream,
                    message,
                    out _,
                    pool,
                    TestContext.Current.CancellationToken
                );
            }
        });

        Assert.Same(failure, exception);
        Assert.Equal(1, pool.Returns);
        Assert.Equal(0, stream.SendCalls);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(9)]
    public void Send_InvalidEncodedLength_ReturnsBufferWithoutSending(int written)
    {
        using var stream = new DeferredStream();
        var pool = new TrackingPool();

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = DataStreamHelper.Send(
                stream,
                new VariableMessage(),
                new TestEncoder { Written = written },
                pool,
                TestContext.Current.CancellationToken
            );
        });

        Assert.Equal(1, pool.Returns);
        Assert.Equal(0, stream.SendCalls);
    }

    [Fact]
    public void Send_InvalidRemainingSpan_ReturnsBufferWithoutSending()
    {
        using var stream = new DeferredStream();
        var pool = new TrackingPool();

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = DataStreamHelper.Send(
                stream,
                new VariableMessage { ExpandRemainingSpan = true },
                out _,
                pool,
                TestContext.Current.CancellationToken
            );
        });

        Assert.Equal(1, pool.Returns);
        Assert.Equal(0, stream.SendCalls);
    }

    private sealed class VariableMessage : IProtocolMessage
    {
        private ProtocolTags _tags;
        public int SerializeCalls { get; private set; }
        public Exception? Failure { get; init; }
        public bool ExpandRemainingSpan { get; init; }
        public ProtocolInfo Protocol => ExampleProtocol.Info;
        public string Name => nameof(VariableMessage);
        public ref ProtocolTags Tags => ref _tags;

        public string GetIdAsString() => "variable";

        public int GetByteSize() => 8;

        public void Serialize(ref Span<byte> buffer)
        {
            SerializeCalls++;
            if (Failure != null)
            {
                throw Failure;
            }

            if (ExpandRemainingSpan)
            {
                buffer = new byte[buffer.Length + 1];
                return;
            }

            buffer[0] = 1;
            buffer[1] = 2;
            buffer[2] = 3;
            buffer = buffer[3..];
        }

        public void Deserialize(ref ReadOnlySpan<byte> buffer) => throw new NotSupportedException();
    }

    private sealed class TestEncoder : IProtocolEncoder
    {
        public int EncodeCalls { get; private set; }
        public int DestinationLength { get; private set; }
        public int Written { get; init; } = 2;
        public Exception? Failure { get; init; }

        public int GetMaxByteSize(IProtocolMessage message) => 8;

        public int Encode(IProtocolMessage message, Span<byte> destination)
        {
            EncodeCalls++;
            DestinationLength = destination.Length;
            if (Failure != null)
            {
                throw Failure;
            }

            destination[0] = 7;
            destination[1] = 8;
            return Written;
        }
    }

    private sealed class TrackingPool : ArrayPool<byte>
    {
        public byte[] Buffer { get; } = new byte[32];
        public int Rents { get; private set; }
        public int Returns { get; private set; }

        public override byte[] Rent(int minimumLength)
        {
            Assert.InRange(minimumLength, 0, Buffer.Length);
            Rents++;
            Buffer.AsSpan().Fill(0xCC);
            return Buffer;
        }

        public override void Return(byte[] array, bool clearArray = false)
        {
            Assert.Same(Buffer, array);
            Returns++;
            array.AsSpan().Fill(0xEE);
        }
    }

    private sealed class DeferredStream : IDataStream, IDisposable
    {
        private readonly Subject<byte[]> _receive = new();
        private readonly TaskCompletionSource<bool> _completion = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        public byte[]? Buffer { get; private set; }
        public int Count { get; private set; }
        public int SendCalls { get; private set; }
        public CancellationToken Cancellation { get; private set; }
        public Exception? SynchronousFailure { get; init; }
        public Observable<byte[]> OnReceive => _receive;
        public string Name => "deferred";
        public long RxBytes => 0;
        public long TxBytes => Count;

        public Task<bool> Send(byte[] data, int count, CancellationToken cancel)
        {
            SendCalls++;
            if (SynchronousFailure != null)
            {
                throw SynchronousFailure;
            }

            Buffer = data;
            Count = count;
            Cancellation = cancel;
            return _completion.Task;
        }

        public Task<bool> Send(ReadOnlyMemory<byte> data, CancellationToken cancel) =>
            throw new NotSupportedException();

        public byte[] GetPendingBytes() => Buffer!.AsSpan(0, Count).ToArray();

        public void Complete(bool success) => _completion.SetResult(success);

        public void Fail(Exception exception) => _completion.SetException(exception);

        public void Cancel() => _completion.SetCanceled();

        public void Dispose() => _receive.Dispose();
    }
}
