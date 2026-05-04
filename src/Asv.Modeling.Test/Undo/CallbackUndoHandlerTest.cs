using System.Buffers;
using JetBrains.Annotations;
using R3;

namespace Asv.Modeling.Test;

[TestSubject(typeof(CallbackUndoHandler<>))]
public class CallbackUndoHandlerTest
{
    [Fact]
    public void Create_ReturnsNewChangeInstance()
    {
        using var publicator = new Subject<TestChange>();
        var handler = new CallbackUndoHandler<TestChange>(
            "change",
            (_, _) => ValueTask.CompletedTask,
            (_, _) => ValueTask.CompletedTask,
            publicator
        );

        var change = handler.Create();

        Assert.IsType<TestChange>(change);
        Assert.Equal(default, (TestChange)change);
    }

    [Fact]
    public async ValueTask Undo_InvokesCallbackWithTypedChangeAndCancellationToken()
    {
        using var publicator = new Subject<TestChange>();
        var expected = new TestChange { Value = 42 };
        using var cts = new CancellationTokenSource();
        TestChange? actualChange = null;
        CancellationToken actualToken = default;
        CallbackUndoHandler<TestChange>? handler = null;

        handler = new CallbackUndoHandler<TestChange>(
            "change",
            (change, cancel) =>
            {
                actualChange = change;
                actualToken = cancel;
                Assert.True(handler!.MuteChanges);
                return ValueTask.CompletedTask;
            },
            (_, _) => ValueTask.CompletedTask,
            publicator
        );

        await handler.Undo(expected, cts.Token);

        Assert.Equal(expected, actualChange);
        Assert.Equal(cts.Token, actualToken);
        Assert.False(handler.MuteChanges);
    }

    [Fact]
    public async ValueTask Redo_InvokesCallbackWithTypedChangeAndCancellationToken()
    {
        using var publicator = new Subject<TestChange>();
        var expected = new TestChange { Value = 43 };
        using var cts = new CancellationTokenSource();
        TestChange? actualChange = null;
        CancellationToken actualToken = default;
        CallbackUndoHandler<TestChange>? handler = null;

        handler = new CallbackUndoHandler<TestChange>(
            "change",
            (_, _) => ValueTask.CompletedTask,
            (change, cancel) =>
            {
                actualChange = change;
                actualToken = cancel;
                Assert.True(handler!.MuteChanges);
                return ValueTask.CompletedTask;
            },
            publicator
        );

        await handler.Redo(expected, cts.Token);

        Assert.Equal(expected, actualChange);
        Assert.Equal(cts.Token, actualToken);
        Assert.False(handler.MuteChanges);
    }

    [Fact]
    public void Changes_PublishesIncomingChangesAsIChange_WhenNotMuted()
    {
        using var publicator = new Subject<TestChange>();
        var handler = new CallbackUndoHandler<TestChange>(
            "change",
            (_, _) => ValueTask.CompletedTask,
            (_, _) => ValueTask.CompletedTask,
            publicator
        );
        var received = new List<IChange>();
        using var subscription = handler.Changes.Subscribe(received.Add);

        var first = new TestChange { Value = 1 };
        var second = new TestChange { Value = 2 };

        publicator.OnNext(first);
        handler.MuteChanges = true;
        publicator.OnNext(second);
        handler.MuteChanges = false;

        Assert.Single(received);
        Assert.Equal(first, Assert.IsType<TestChange>(received[0]));
    }

    [Fact]
    public async ValueTask Undo_RestoresMuteChanges_WhenCallbackThrows()
    {
        using var publicator = new Subject<TestChange>();
        var expected = new InvalidOperationException("undo failed");
        var handler = new CallbackUndoHandler<TestChange>(
            "change",
            (_, _) => throw expected,
            (_, _) => ValueTask.CompletedTask,
            publicator
        );

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await handler.Undo(new TestChange(), TestContext.Current.CancellationToken)
        );

        Assert.Same(expected, actual);
        Assert.False(handler.MuteChanges);
    }

    private struct TestChange : IChange
    {
        public int Value { get; set; }

        public void Serialize(IBufferWriter<byte> writer)
        {
        }

        public void Deserialize(ReadOnlySequence<byte> data)
        {
        }
    }
}
