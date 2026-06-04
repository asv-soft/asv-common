using ObservableCollections;

namespace Asv.Modeling.Test;

public class ObservableMixinTest
{
    [Fact]
    public void PopulateTo_DisposeAfterSynchronizationContextShutdown_DoesNotSendToContext()
    {
        // Arrange
        var src = new ObservableList<int> { 1 };
        var dest = new List<TestWidget>();
        var synchronizationContext = new ShutdownAwareSynchronizationContext();

        var subscription = src.PopulateTo(
            dest,
            value => new TestWidget(value),
            (value, widget) => widget.Value == value,
            synchronizationContext: synchronizationContext
        );

        Assert.Single(dest);

        synchronizationContext.Shutdown();

        // Act
        var exception = Record.Exception(subscription.Dispose);

        // Assert
        Assert.Null(exception);
        Assert.Empty(dest);
        Assert.Equal(0, synchronizationContext.SendCount);
    }

    [Fact]
    public void PopulateTo_CollectionChanges_ObserveOnSynchronizationContext()
    {
        // Arrange
        var src = new ObservableList<int>();
        var dest = new List<TestWidget>();
        var synchronizationContext = new QueueSynchronizationContext();

        using var subscription = src.PopulateTo(
            dest,
            value => new TestWidget(value),
            (value, widget) => widget.Value == value,
            synchronizationContext: synchronizationContext
        );

        // Act
        src.Add(1);

        // Assert
        Assert.Empty(dest);

        // Act
        synchronizationContext.ExecuteQueuedCallbacks();

        // Assert
        var widget = Assert.Single(dest);
        Assert.Equal(1, widget.Value);

        // Act
        src.Remove(1);

        // Assert
        Assert.Single(dest);

        // Act
        synchronizationContext.ExecuteQueuedCallbacks();

        // Assert
        Assert.Empty(dest);
        Assert.True(widget.IsDisposed);
        Assert.Equal(0, synchronizationContext.SendCount);
    }

    private sealed class TestWidget(int value) : IDisposable
    {
        public int Value { get; } = value;

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class ShutdownAwareSynchronizationContext : SynchronizationContext
    {
        private bool _isShutdown;

        public int SendCount { get; private set; }

        public override void Send(SendOrPostCallback d, object? state)
        {
            SendCount++;
            if (_isShutdown)
            {
                throw new InvalidOperationException("Synchronization context was shut down.");
            }

            d(state);
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            d(state);
        }

        public void Shutdown()
        {
            _isShutdown = true;
        }
    }

    private sealed class QueueSynchronizationContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object? State)> _callbacks = new();

        public int SendCount { get; private set; }

        public override void Send(SendOrPostCallback d, object? state)
        {
            SendCount++;
            d(state);
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            _callbacks.Enqueue((d, state));
        }

        public void ExecuteQueuedCallbacks()
        {
            while (_callbacks.TryDequeue(out var callback))
            {
                callback.Callback(callback.State);
            }
        }
    }
}
