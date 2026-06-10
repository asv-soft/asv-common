using System.Buffers;
using JetBrains.Annotations;
using R3;

namespace Asv.Modeling.Test;

[TestSubject(typeof(ValueUndoChangeMixin))]
public class ValueUndoChangeMixinTest
{
    [Fact]
    public async ValueTask CreateValueChange_WithAsyncCallbacks_UsesOldValueForUndoAndNewValueForRedo()
    {
        using var controller = new UndoController<IViewModel>(new EmptyViewModel("root"));
        string? undoValue = null;
        string? redoValue = null;

        var sink = controller.RegisterValue<string>(
            "value",
            (value, _) =>
            {
                undoValue = value;
                return ValueTask.CompletedTask;
            },
            (value, _) =>
            {
                redoValue = value;
                return ValueTask.CompletedTask;
            }
        );

        sink.PublishUpdate("old", "new");

        var handler = controller["value"];
        var change = new ValueUndoChange<string>
        {
            Operation = ChangeOperation.Update,
            OldValue = "old",
            NewValue = "new",
        };

        await handler.Undo(change, TestContext.Current.CancellationToken);
        await handler.Redo(change, TestContext.Current.CancellationToken);

        Assert.Equal("old", undoValue);
        Assert.Equal("new", redoValue);
    }

    [Fact]
    public async ValueTask CreateValueChange_WithSyncCallbacks_UsesOldValueForUndoAndNewValueForRedo()
    {
        using var controller = new UndoController<IViewModel>(new EmptyViewModel("root"));
        var undoValue = 0;
        var redoValue = 0;

        var sink = controller.RegisterValue<int>(
            "value",
            value => undoValue = value,
            value => redoValue = value
        );

        sink.PublishUpdate((1, 2));

        var handler = controller["value"];
        var change = new ValueUndoChange<int>
        {
            Operation = ChangeOperation.Update,
            OldValue = 1,
            NewValue = 2,
        };

        await handler.Undo(change, TestContext.Current.CancellationToken);
        await handler.Redo(change, TestContext.Current.CancellationToken);

        Assert.Equal(1, undoValue);
        Assert.Equal(2, redoValue);
    }

    [Fact]
    public async ValueTask Create_WithSyncUndoCallbacks_RegistersHandler()
    {
        using var controller = new UndoController<IViewModel>(new EmptyViewModel("root"));
        ValueUndoChange<int>? undoChange = null;
        ValueUndoChange<int>? redoChange = null;

        controller.Register<ValueUndoChange<int>>(
            "value",
            change => undoChange = change,
            change => redoChange = change
        );

        var change = new ValueUndoChange<int>
        {
            Operation = ChangeOperation.Update,
            OldValue = 1,
            NewValue = 2,
        };
        var handler = controller["value"];

        await handler.Undo(change, TestContext.Current.CancellationToken);
        await handler.Redo(change, TestContext.Current.CancellationToken);

        Assert.Equal(1, undoChange?.OldValue);
        Assert.Equal(2, redoChange?.NewValue);
    }

    [Fact]
    public async ValueTask TrackProperty_WithEnumViewAndStoreConverter_SavesConvertedStoreValues()
    {
        var store = new RecordingUndoHistoryStore();
        using var root = new EnumHistoryRoot(store);
        using var child = new EnumPropertyViewModel("child");
        root.AddChild(child);

        child.State.Value = TrackedState.Active;

        await root.UndoHistory.UndoAsync(TestContext.Current.CancellationToken);

        var storedChange = ReadLastStoredValueChange(store);
        Assert.Equal(ChangeOperation.Update, storedChange.Operation);
        Assert.Equal((int)TrackedState.None, storedChange.OldValue);
        Assert.Equal((int)TrackedState.Active, storedChange.NewValue);
    }

    [Fact]
    public async ValueTask TrackProperty_WithEnumViewAndStoreConverter_RestoresEnumValuesOnUndoRedo()
    {
        var store = new RecordingUndoHistoryStore();
        using var root = new EnumHistoryRoot(store);
        using var child = new EnumPropertyViewModel("child");
        root.AddChild(child);

        child.State.Value = TrackedState.Armed;

        await root.UndoHistory.UndoAsync(TestContext.Current.CancellationToken);
        Assert.Equal(TrackedState.None, (TrackedState)child.State.Value);

        await root.UndoHistory.RedoAsync(TestContext.Current.CancellationToken);
        Assert.Equal(TrackedState.Armed, (TrackedState)child.State.Value);
    }

    [Fact]
    public void Publish_WithOldAndNewValues_PublishesUpdateChange()
    {
        var sink = new TestUndoChangeSink<int>();

        sink.PublishUpdate(1, 2);

        var change = Assert.Single(sink.Changes);
        Assert.Equal(ChangeOperation.Update, change.Operation);
        Assert.Equal(1, change.OldValue);
        Assert.Equal(2, change.NewValue);
    }

    [Fact]
    public void Publish_WithEqualOldAndNewValues_DoesNotPublishUpdateChange()
    {
        var sink = new TestUndoChangeSink<int>();

        sink.PublishUpdate(1, 1);

        Assert.Empty(sink.Changes);
    }

    [Fact]
    public void Publish_WithOperationAndValues_PublishesChange()
    {
        var sink = new TestUndoChangeSink<string>();

        sink.PublishUpdate(ChangeOperation.Create, "", "created");

        var change = Assert.Single(sink.Changes);
        Assert.Equal(ChangeOperation.Create, change.Operation);
        Assert.Equal("", change.OldValue);
        Assert.Equal("created", change.NewValue);
    }

    [Fact]
    public void Publish_WithTuple_PublishesUpdateChange()
    {
        var sink = new TestUndoChangeSink<int>();

        sink.PublishUpdate((10, 20));

        var change = Assert.Single(sink.Changes);
        Assert.Equal(ChangeOperation.Update, change.Operation);
        Assert.Equal(10, change.OldValue);
        Assert.Equal(20, change.NewValue);
    }

    [Fact]
    public void Publish_WithOperationAndTuple_PublishesChange()
    {
        var sink = new TestUndoChangeSink<int>();

        sink.PublishUpdate(ChangeOperation.Delete, (10, 0));

        var change = Assert.Single(sink.Changes);
        Assert.Equal(ChangeOperation.Delete, change.Operation);
        Assert.Equal(10, change.OldValue);
        Assert.Equal(0, change.NewValue);
    }

    private static ValueUndoChange<int> ReadLastStoredValueChange(RecordingUndoHistoryStore store)
    {
        var change = new ValueUndoChange<int>();
        change.Deserialize(new ReadOnlySequence<byte>(store.LastSavedData));
        return change;
    }

    private enum TrackedState
    {
        None = 0,
        Armed = 1,
        Active = 2,
    }

    private sealed class EnumHistoryRoot : ViewModelBase, ISupportUndoHistory<IViewModel>
    {
        private readonly List<IViewModel> _children = [];

        public EnumHistoryRoot(IUndoHistoryStore store)
            : base("root")
        {
            UndoHistory = new UndoHistory<IViewModel>(this, store).AddTo(ref DisposableBag);
        }

        public IUndoHistory<IViewModel> UndoHistory { get; }

        public void AddChild(IViewModel child)
        {
            child.SetParent(this);
            _children.Add(child);
        }

        public override IEnumerable<IViewModel> GetChildren()
        {
            return _children;
        }
    }

    private sealed class EnumPropertyViewModel : UndoableViewModel
    {
        public EnumPropertyViewModel(string id)
            : base(id)
        {
            Undo.TrackProperty<Enum, int>(
                    nameof(State),
                    State,
                    value => (TrackedState)value,
                    value => (int)(TrackedState)value
                )
                .AddTo(ref DisposableBag);
        }

        public ReactiveProperty<Enum> State { get; } = new(TrackedState.None);

        public override IEnumerable<IViewModel> GetChildren()
        {
            return [];
        }
    }

    private sealed class RecordingUndoHistoryStore : IUndoHistoryStore
    {
        private readonly Dictionary<IUndoSnapshot, byte[]> _data = [];

        public byte[] LastSavedData { get; private set; } = [];

        public void LoadUndoRedo(Action<IUndoSnapshot> addUndo, Action<IUndoSnapshot> addRedo) { }

        public void SaveUndoRedo(
            IEnumerable<IUndoSnapshot> undo,
            IEnumerable<IUndoSnapshot> redo
        ) { }

        public IUndoSnapshot CreateSnapshot(NavPath path, string changeId)
        {
            return new RecordingUndoSnapshot(path, changeId);
        }

        public void LoadChange(IUndoSnapshot snapshot, IUndoChange undoChange)
        {
            undoChange.Deserialize(new ReadOnlySequence<byte>(_data[snapshot]));
        }

        public void SaveChange(IUndoSnapshot snapshot, IUndoChange undoChange)
        {
            var writer = new ArrayBufferWriter<byte>();
            undoChange.Serialize(writer);
            LastSavedData = writer.WrittenMemory.ToArray();
            _data[snapshot] = LastSavedData;
        }

        public void Dispose() { }

        private sealed class RecordingUndoSnapshot(NavPath path, string changeId) : IUndoSnapshot
        {
            public NavPath Path { get; } = path;

            public string ChangeId { get; } = changeId;
        }
    }

    private sealed class TestUndoChangeSink<T> : IUndoChangeSink<ValueUndoChange<T>>
    {
        private int _suppressChangePublicationCount;

        public List<ValueUndoChange<T>> Changes { get; } = [];

        public IDisposable SuppressChangePublication()
        {
            Interlocked.Increment(ref _suppressChangePublicationCount);
            return R3.Disposable.Create(
                this,
                static sink => Interlocked.Decrement(ref sink._suppressChangePublicationCount)
            );
        }

        public void Publish(ValueUndoChange<T> change)
        {
            if (_suppressChangePublicationCount > 0)
                return;
            Changes.Add(change);
        }

        public void Dispose() { }
    }

    private sealed class EmptyViewModel(string id) : ViewModelBase(id)
    {
        public override IEnumerable<IViewModel> GetChildren()
        {
            return [];
        }
    }
}
