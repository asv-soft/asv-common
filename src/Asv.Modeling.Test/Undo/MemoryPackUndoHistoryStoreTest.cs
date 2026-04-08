using System.Buffers;
using MemoryPack;
using R3;

namespace Asv.Modeling.Test;

public class JsonUndoHistoryStoreTest
{
    private static readonly BinaryChangeHandler BinaryHandler = new("binary");

    [Fact]
    public void SaveChange_SmallPayload_StoresDataInSnapshot()
    {
        var storageDir = Path.Combine(
            Path.GetTempPath(),
            "asv-undo-test",
            Guid.NewGuid().ToString("N")
        );
        using var store = new JsonUndoHistoryStore<string>(storageDir);

        var snapshot = (UndoSnapshot<string>)store.CreateSnapshot(["root", "child"], "change-1");
        var payload = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        store.SaveChange(snapshot.DataRefId, BinaryHandler.Serialize(new BinaryChange(payload)));

        Assert.NotNull(snapshot.Data);
        Assert.Equal(payload, snapshot.Data);

        var loaded = (BinaryChange)BinaryHandler.Deserialize(store.LoadChange(snapshot.DataRefId));
        Assert.Equal(payload, loaded.Data);
    }

    [Fact]
    public void SaveChange_LargePayload_StoresDataInFile()
    {
        var storageDir = Path.Combine(
            Path.GetTempPath(),
            "asv-undo-test",
            Guid.NewGuid().ToString("N")
        );
        using var store = new JsonUndoHistoryStore<string>(storageDir, inMemoryThresholdBytes: 16);

        var snapshot = (UndoSnapshot<string>)store.CreateSnapshot(["root"], "change-2");
        var payload = Enumerable.Range(1, 256).Select(x => (byte)x).ToArray();
        store.SaveChange(snapshot.DataRefId, BinaryHandler.Serialize(new BinaryChange(payload)));

        var filePath = Path.Combine(storageDir, $"{snapshot.DataRefId:N}.undo");
        Assert.Null(snapshot.Data);
        Assert.True(File.Exists(filePath));

        var loaded = (BinaryChange)BinaryHandler.Deserialize(store.LoadChange(snapshot.DataRefId));
        Assert.Equal(payload, loaded.Data);
    }

    [Fact]
    public void SaveAndLoadStacks_PersistAsJsonL()
    {
        var storageDir = Path.Combine(
            Path.GetTempPath(),
            "asv-undo-test",
            Guid.NewGuid().ToString("N")
        );
        using (
            var store = new JsonUndoHistoryStore<string>(storageDir, inMemoryThresholdBytes: 128)
        )
        {
            var undoSnapshot = (UndoSnapshot<string>)store.CreateSnapshot(["u1"], "undo-change");
            store.SaveChange(
                undoSnapshot.DataRefId,
                BinaryHandler.Serialize(new BinaryChange([1, 2, 3]))
            );
            store.SaveUndoStack([undoSnapshot]);

            var redoSnapshot = (UndoSnapshot<string>)store.CreateSnapshot(["r1"], "redo-change");
            store.SaveChange(
                redoSnapshot.DataRefId,
                BinaryHandler.Serialize(new BinaryChange([4, 5, 6]))
            );
            store.SaveRedoStack([redoSnapshot]);
        }

        var undoJsonl = Path.Combine(storageDir, "undo-stack.jsonl");
        var redoJsonl = Path.Combine(storageDir, "redo-stack.jsonl");
        Assert.True(File.Exists(undoJsonl));
        Assert.True(File.Exists(redoJsonl));
        Assert.NotEmpty(File.ReadAllLines(undoJsonl).Where(x => !string.IsNullOrWhiteSpace(x)));
        Assert.NotEmpty(File.ReadAllLines(redoJsonl).Where(x => !string.IsNullOrWhiteSpace(x)));

        using var reloadedStore = new JsonUndoHistoryStore<string>(
            storageDir,
            inMemoryThresholdBytes: 128
        );
        var undo = reloadedStore.LoadUndoStack().Cast<UndoSnapshot<string>>().ToArray();
        var redo = reloadedStore.LoadRedoStack().Cast<UndoSnapshot<string>>().ToArray();

        Assert.Single(undo);
        Assert.Single(redo);
        Assert.Equal("undo-change", undo[0].ChangeId);
        Assert.Equal("redo-change", redo[0].ChangeId);

        var loadedUndo = (BinaryChange)
            BinaryHandler.Deserialize(reloadedStore.LoadChange(undo[0].DataRefId));
        Assert.Equal([1, 2, 3], loadedUndo.Data);
    }

    private sealed class BinaryChange : IChange
    {
        public BinaryChange()
        {
            Data = [];
        }

        public BinaryChange(byte[] data)
        {
            Data = data;
        }

        public byte[] Data { get; private set; }

        public void Serialize(IBufferWriter<byte> writer)
        {
            MemoryPackSerializer.Serialize(writer, this);
        }

        public void Deserialize(ReadOnlySequence<byte> data)
        {
            MemoryPackSerializer.Deserialize(in data, ref this);
        }
    }

    private sealed class BinaryChangeHandler(string id)
        : UndoChangeHandler<BinaryChange>(id, Observable.Empty<IChange>())
    {
        public override IChange Create()
        {
            return new BinaryChange();
        }

        protected override ValueTask InternalUndo(BinaryChange change, CancellationToken cancel) =>
            ValueTask.CompletedTask;

        protected override ValueTask InternalRedo(BinaryChange change, CancellationToken cancel) =>
            ValueTask.CompletedTask;
    }
}
