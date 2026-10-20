using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using R3;
using Xunit;
using static Asv.IO.Test.Encoder.EncoderTestSupport;

namespace Asv.IO.Test.Encoder;

public class ProtocolEndpointEncoderTest
{
    [Fact]
    public async Task VirtualPort_LegacyContextSendsOnlySerializedBytes()
    {
        var context = new LegacyContext(CreateContext());
        await using var port = new VirtualPort("legacy", context, _ => true, new Statistic());
        var frames = new List<byte[]>();
        using var subscription = port.Tx.Subscribe(frames.Add);
        var message = new TestMessage(42, 8);

        await port.Send(message, TestContext.Current.CancellationToken);

        Assert.Equal(new byte[] { 42 }, Assert.Single(frames));
        Assert.Equal(1, message.SerializeCalls);
        Assert.Equal(1U, port.Statistic.TxBytes);
        Assert.Equal(1U, port.Statistic.TxMessages);
    }

    [Fact]
    public async Task VirtualPort_EncodingFailureReportsErrorAndStopsStream()
    {
        var failure = new InvalidOperationException("Simulated encoding failure.");
        var encoder = new ThrowingEncoder(failure);
        var context = CreateContext(_ => encoder);
        await using var port = new VirtualPort("failed", context, _ => true, new Statistic());
        var frames = new List<byte[]>();
        var errors = new List<Exception>();
        using var framesSubscription = port.Tx.Subscribe(frames.Add);
        using var errorsSubscription = port.OnTxError.Subscribe(errors.Add);

        await port.Send(new TestMessage(1), TestContext.Current.CancellationToken);
        await port.Send(new TestMessage(2), TestContext.Current.CancellationToken);

        Assert.Equal(1, encoder.EncodeCalls);
        Assert.Empty(frames);
        var error = Assert.IsType<ProtocolConnectionException>(Assert.Single(errors));
        Assert.Same(error, port.LastError.CurrentValue);
        Assert.Same(failure, error.InnerException);
        Assert.Equal(1U, port.Statistic.TxError);
        Assert.Equal(0U, port.Statistic.TxMessages);
    }

    [Fact]
    public async Task VirtualPorts_SharedMessageHasIndependentStreamState()
    {
        var factoryCalls = 0;
        var context = CreateContext(_ =>
        {
            Interlocked.Increment(ref factoryCalls);
            return new StatefulEncoder();
        });
        await using var first = new VirtualPort("first", context, _ => true, new Statistic());
        await using var second = new VirtualPort("second", context, _ => true, new Statistic());
        var firstFrames = new List<byte[]>();
        var secondFrames = new List<byte[]>();
        using var firstSubscription = first.Tx.Subscribe(firstFrames.Add);
        using var secondSubscription = second.Tx.Subscribe(secondFrames.Add);
        var message = new TestMessage(42);

        await first.Send(message, TestContext.Current.CancellationToken);
        await first.Send(message, TestContext.Current.CancellationToken);
        await second.Send(message, TestContext.Current.CancellationToken);

        Assert.Equal(2, factoryCalls);
        Assert.Equal(new byte[] { 0, 42 }, firstFrames[0]);
        Assert.Equal(new byte[] { 1, 42 }, firstFrames[1]);
        Assert.Equal(new byte[] { 0, 42 }, Assert.Single(secondFrames));
        Assert.Equal(0, message.SerializeCalls);
        Assert.Equal(4U, first.Statistic.TxBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task VirtualPort_ConcurrentSendsSerializeFilterEncodeAndEmission(
        bool cancellableCaller
    )
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var filterCalls = 0;
        var feature = new TestFeature(async message =>
        {
            if (Interlocked.Increment(ref filterCalls) == 1)
            {
                entered.SetResult();
                await release.Task;
            }
            return message;
        });
        var encoder = new StatefulEncoder();
        var context = CreateContext(_ => encoder, feature);
        await using var port = new VirtualPort("ordered", context, _ => true, new Statistic());
        var frames = new List<byte[]>();
        using var subscription = port.Tx.Subscribe(frames.Add);
        using var cancellation = new CancellationTokenSource();
        var cancel = cancellableCaller ? cancellation.Token : CancellationToken.None;

        var first = port.Send(new TestMessage(11), cancel).AsTask();
        await Wait(entered.Task);
        var second = port.Send(new TestMessage(22), cancel).AsTask();
        try
        {
            Assert.Equal(1, Volatile.Read(ref filterCalls));
            Assert.False(second.IsCompleted);
            Assert.Empty(frames);
            Assert.Equal(0, encoder.EncodeCalls);
        }
        finally
        {
            release.TrySetResult();
        }
        await Wait(Task.WhenAll(first, second));

        Assert.Equal(2, encoder.EncodeCalls);
        Assert.Equal(new byte[] { 0, 11 }, frames[0]);
        Assert.Equal(new byte[] { 1, 22 }, frames[1]);
    }

    [Fact]
    public async Task VirtualPort_FilteredAndCancelledMessagesDoNotAdvanceState()
    {
        var encoder = new StatefulEncoder();
        var feature = new TestFeature(message =>
            ValueTask.FromResult<IProtocolMessage?>(
                ((TestMessage)message).Payload[0] == 2 ? null : message
            )
        );
        var context = CreateContext(_ => encoder, feature);
        await using var port = new VirtualPort(
            "filtered",
            context,
            message => ((TestMessage)message).Payload[0] != 1,
            new Statistic()
        );
        var frames = new List<byte[]>();
        using var subscription = port.Tx.Subscribe(frames.Add);

        await port.Send(new TestMessage(1), TestContext.Current.CancellationToken);
        await port.Send(new TestMessage(2), TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await port.Send(new TestMessage(3), new CancellationToken(true))
        );
        await port.Send(new TestMessage(4), TestContext.Current.CancellationToken);

        Assert.Equal(1, encoder.EncodeCalls);
        Assert.Equal(new byte[] { 0, 4 }, Assert.Single(frames));
    }

    [Fact]
    public async Task VirtualPort_CancellationWhileWaitingDoesNotConsumeStreamState()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var feature = new TestFeature(async message =>
        {
            if (((TestMessage)message).Payload[0] == 1)
            {
                entered.SetResult();
                await release.Task;
            }
            return message;
        });
        var encoder = new StatefulEncoder();
        var context = CreateContext(_ => encoder, feature);
        await using var port = new VirtualPort("cancelled", context, _ => true, new Statistic());
        var frames = new List<byte[]>();
        using var subscription = port.Tx.Subscribe(frames.Add);
        using var cancellation = new CancellationTokenSource();

        var first = port.Send(new TestMessage(1), TestContext.Current.CancellationToken).AsTask();
        await Wait(entered.Task);
        var waiting = port.Send(new TestMessage(2), cancellation.Token).AsTask();
        try
        {
            await cancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Wait(waiting));
        }
        finally
        {
            release.TrySetResult();
        }
        await Wait(first);
        await port.Send(new TestMessage(3), TestContext.Current.CancellationToken);

        Assert.Equal(2, encoder.EncodeCalls);
        Assert.Equal(new byte[] { 0, 1 }, frames[0]);
        Assert.Equal(new byte[] { 1, 3 }, frames[1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task VirtualPort_CancellationDuringFilterDoesNotConsumeStateOrHoldGate(
        bool waitForGate
    )
    {
        var blockerEntered = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var releaseBlocker = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var filterEntered = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var releaseFilter = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var feature = new TestFeature(async message =>
        {
            switch (((TestMessage)message).Payload[0])
            {
                case 1:
                    blockerEntered.SetResult();
                    await releaseBlocker.Task;
                    break;
                case 2:
                    filterEntered.SetResult();
                    await releaseFilter.Task;
                    break;
            }
            return message;
        });
        var encoder = new StatefulEncoder();
        await using var port = new VirtualPort(
            "filter-cancel",
            CreateContext(_ => encoder, feature),
            _ => true,
            new Statistic()
        );
        var frames = new List<byte[]>();
        using var subscription = port.Tx.Subscribe(frames.Add);
        using var cancellation = new CancellationTokenSource();

        var blocker = Task.CompletedTask;
        Task pending;
        try
        {
            if (waitForGate)
            {
                blocker = port.Send(new TestMessage(1)).AsTask();
                await Wait(blockerEntered.Task);
            }
            pending = port.Send(new TestMessage(2), cancellation.Token).AsTask();
            if (waitForGate)
            {
                Assert.False(filterEntered.Task.IsCompleted);
            }
            releaseBlocker.TrySetResult();
            await Wait(blocker);
            await Wait(filterEntered.Task);
            await cancellation.CancelAsync();
        }
        finally
        {
            releaseBlocker.TrySetResult();
            releaseFilter.TrySetResult();
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Wait(pending));
        await Wait(port.Send(new TestMessage(3)).AsTask());

        var expectedSequence = waitForGate ? 1 : 0;
        Assert.Equal(expectedSequence + 1, encoder.EncodeCalls);
        Assert.Equal(expectedSequence + 1, frames.Count);
        Assert.Equal(new byte[] { (byte)expectedSequence, 3 }, frames[^1]);
        Assert.Equal(0U, port.Statistic.TxError);
        Assert.Null(port.LastError.CurrentValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task VirtualPort_DisposalCancelsWaitingAndFilteringSends(bool cancellableCaller)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var feature = new TestFeature(async message =>
        {
            entered.TrySetResult();
            await release.Task;
            return message;
        });
        var encoder = new StatefulEncoder();
        await using var port = new VirtualPort(
            "dispose",
            CreateContext(_ => encoder, feature),
            _ => true,
            new Statistic()
        );
        var frames = new List<byte[]>();
        using var subscription = port.Tx.Subscribe(frames.Add);
        using var cancellation = new CancellationTokenSource();
        var cancel = cancellableCaller ? cancellation.Token : CancellationToken.None;

        var filtering = port.Send(new TestMessage(1), cancel).AsTask();
        try
        {
            await Wait(entered.Task);
            var waiting = port.Send(new TestMessage(2), cancel).AsTask();
            Assert.False(waiting.IsCompleted);

            await port.DisposeAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Wait(waiting));
            Assert.False(cancellation.IsCancellationRequested);
        }
        finally
        {
            release.TrySetResult();
        }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Wait(filtering));

        Assert.Equal(0, encoder.EncodeCalls);
        Assert.Empty(frames);
        Assert.Equal(0U, port.Statistic.TxError);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VirtualPort_UncontendedSendsCompleteSynchronously(bool cancellableCaller)
    {
        var encoder = new StatefulEncoder();
        using var port = new VirtualPort(
            "synchronous",
            CreateContext(_ => encoder),
            _ => true,
            new Statistic()
        );
        var frames = new List<byte[]>();
        using var subscription = port.Tx.Subscribe(frames.Add);
        using var cancellation = new CancellationTokenSource();
        var cancel = cancellableCaller ? cancellation.Token : CancellationToken.None;
        var message = new TestMessage(42);

        for (var i = 0; i < 2; i++)
        {
            var pending = port.Send(message, cancel);
            Assert.True(pending.IsCompletedSuccessfully);
            pending.GetAwaiter().GetResult();
        }

        Assert.Equal(2, encoder.EncodeCalls);
        Assert.Equal(2, frames.Count);
        Assert.Equal(new byte[] { 0, 42 }, frames[0]);
        Assert.Equal(new byte[] { 1, 42 }, frames[1]);
        Assert.Equal(2U, port.Statistic.TxMessages);
        Assert.Equal(0U, port.Statistic.TxError);
    }

    [Fact]
    public async Task PhysicalEndpoint_DefaultEncoderPreservesShortSerialization()
    {
        await using var endpoint = new CapturingEndpoint(new LegacyContext(CreateContext()));
        var message = new TestMessage(42, 8);

        await endpoint.Send(message, TestContext.Current.CancellationToken);
        var write = await endpoint.ReadWrite();
        Assert.Equal(new byte[] { 42 }, write.Bytes);
        write.Complete();

        Assert.Same(message, await endpoint.ReadPublished());
        Assert.Equal(1, message.SerializeCalls);
        Assert.Equal(1U, endpoint.Statistic.TxBytes);
    }

    [Fact]
    public async Task PhysicalEndpoints_SharedMessageHasIndependentStreamState()
    {
        var factoryCalls = 0;
        var context = CreateContext(_ =>
        {
            Interlocked.Increment(ref factoryCalls);
            return new StatefulEncoder();
        });
        await using var first = new CapturingEndpoint(context);
        await using var second = new CapturingEndpoint(context);
        var message = new TestMessage(42);

        await first.Send(message, TestContext.Current.CancellationToken);
        var firstWrite = await first.ReadWrite();
        firstWrite.Complete();
        await first.ReadPublished();
        await first.Send(message, TestContext.Current.CancellationToken);
        var nextWrite = await first.ReadWrite();
        nextWrite.Complete();
        await first.ReadPublished();
        await second.Send(message, TestContext.Current.CancellationToken);
        var secondWrite = await second.ReadWrite();
        secondWrite.Complete();
        await second.ReadPublished();

        Assert.Equal(2, factoryCalls);
        Assert.Equal(new byte[] { 0, 42 }, firstWrite.Bytes);
        Assert.Equal(new byte[] { 1, 42 }, nextWrite.Bytes);
        Assert.Equal(new byte[] { 0, 42 }, secondWrite.Bytes);
        Assert.Equal(0, message.SerializeCalls);
    }

    [Fact]
    public async Task PhysicalEndpoint_FilterReplacementSelectsResultProtocolEncoder()
    {
        var replacementInfo = new ProtocolInfo("replacement", "Replacement protocol");
        var originalEncoder = new StatefulEncoder();
        var replacementEncoder = new StatefulEncoder();
        var replacement = new TestMessage(9) { Protocol = replacementInfo };
        var feature = new TestFeature(_ => ValueTask.FromResult<IProtocolMessage?>(replacement));
        var factory = Protocol.Create(builder =>
        {
            builder.Protocols.Register(Info, (_, _) => new TestParser());
            builder.Protocols.RegisterEncoder(Info, _ => originalEncoder);
            builder.Protocols.RegisterEncoder(replacementInfo, _ => replacementEncoder);
            builder.Features.Register(feature);
        });
        var context = (IProtocolContext)factory;
        await using var endpoint = new CapturingEndpoint(context);
        var original = new TestMessage(1);

        await endpoint.Send(original, TestContext.Current.CancellationToken);
        var write = await endpoint.ReadWrite();
        write.Complete();

        Assert.Same(replacement, await endpoint.ReadPublished());
        Assert.Equal(new byte[] { 0, 9 }, write.Bytes);
        Assert.Equal(0, originalEncoder.EncodeCalls);
        Assert.Equal(1, replacementEncoder.EncodeCalls);
        Assert.Equal(0, original.SerializeCalls);
    }

    [Fact]
    public async Task PhysicalEndpoint_ConcurrentSendsEncodeInWireOrder()
    {
        var encoder = new StatefulEncoder();
        await using var endpoint = new CapturingEndpoint(CreateContext(_ => encoder));
        var sends = Enumerable
            .Range(0, 16)
            .Select(index =>
                Task.Run(async () =>
                    await endpoint.Send(
                        new TestMessage((byte)index),
                        TestContext.Current.CancellationToken
                    )
                )
            )
            .ToArray();
        await Wait(Task.WhenAll(sends));
        var values = new List<byte>();

        for (var index = 0; index < sends.Length; index++)
        {
            var write = await endpoint.ReadWrite();
            Assert.Equal(2, write.Bytes.Length);
            Assert.Equal((byte)index, write.Bytes[0]);
            values.Add(write.Bytes[1]);
            write.Complete();
            await endpoint.ReadPublished();
        }

        Assert.Equal(Enumerable.Range(0, 16).Select(value => (byte)value), values.Order());
        Assert.Equal(16, encoder.EncodeCalls);
        Assert.Equal(32U, endpoint.Statistic.TxBytes);
    }

    [Fact]
    public async Task PhysicalEndpoint_FilteredDroppedAndCancelledMessagesDoNotAdvanceState()
    {
        var encoder = new StatefulEncoder();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var feature = new TestFeature(message =>
        {
            if (((TestMessage)message).Payload[0] != 0)
            {
                return ValueTask.FromResult<IProtocolMessage?>(message);
            }
            entered.TrySetResult();
            return ValueTask.FromResult<IProtocolMessage?>(null);
        });
        var context = CreateContext(_ => encoder, feature);
        var config = new ProtocolPortConfig(new Uri("test://localhost"))
        {
            TxQueueSize = 1,
            DropMessageWhenFullTxQueue = true,
        };
        await using var endpoint = new CapturingEndpoint(context, config);
        var filtered = new TestMessage(0);
        await endpoint.Send(filtered, TestContext.Current.CancellationToken);
        await Wait(entered.Task);

        await endpoint.Send(new TestMessage(1), TestContext.Current.CancellationToken);
        var firstWrite = await endpoint.ReadWrite();
        await endpoint.Send(new TestMessage(2), TestContext.Current.CancellationToken);
        await endpoint.Send(new TestMessage(3), TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await endpoint.Send(new TestMessage(4), new CancellationToken(true))
        );
        firstWrite.Complete();
        await endpoint.ReadPublished();
        var secondWrite = await endpoint.ReadWrite();
        secondWrite.Complete();
        await endpoint.ReadPublished();

        Assert.Equal(new byte[] { 0, 1 }, firstWrite.Bytes);
        Assert.Equal(new byte[] { 1, 3 }, secondWrite.Bytes);
        Assert.Equal(2, encoder.EncodeCalls);
        Assert.Equal(1U, endpoint.Statistic.DroppedTxMessages);
        Assert.Equal(0, filtered.SerializeCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PhysicalEndpoint_WriteFailureStopsFurtherEncoding(bool shortWrite)
    {
        var encoder = new StatefulEncoder();
        await using var endpoint = new CapturingEndpoint(CreateContext(_ => encoder));
        var failed = new TaskCompletionSource<ProtocolConnectionException>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        using var subscription = endpoint.LastError.Subscribe(error =>
        {
            if (error != null)
            {
                failed.TrySetResult(error);
            }
        });

        await endpoint.Send(new TestMessage(1), TestContext.Current.CancellationToken);
        var write = await endpoint.ReadWrite();
        await endpoint.Send(new TestMessage(2), TestContext.Current.CancellationToken);
        if (shortWrite)
        {
            write.Completion.SetResult(write.Bytes.Length - 1);
        }
        else
        {
            write.Completion.SetException(new IOException("Simulated transport failure."));
        }
        await Wait(failed.Task);
        await endpoint.Send(new TestMessage(3), TestContext.Current.CancellationToken);

        Assert.Equal(1, encoder.EncodeCalls);
        Assert.Equal(1, endpoint.WriteCalls);
        Assert.Equal(1U, endpoint.Statistic.TxError);
        Assert.Equal(0U, endpoint.Statistic.TxMessages);
    }

    private sealed class ThrowingEncoder(Exception failure) : IProtocolEncoder
    {
        public int EncodeCalls { get; private set; }

        public int GetMaxByteSize(IProtocolMessage message) => 8;

        public int Encode(IProtocolMessage message, Span<byte> destination)
        {
            EncodeCalls++;
            throw failure;
        }
    }
}
