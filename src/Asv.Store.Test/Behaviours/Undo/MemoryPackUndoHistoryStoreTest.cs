using System.Buffers;
using Asv.Common;
using Asv.Store.Undo.History.Store;
using Xunit;

namespace Asv.Common.Test.Behaviours.Undo;

public class MemoryPackUndoHistoryStoreTest
{
    [Fact]
    public void SaveChange_SmallPayload_StoresDataInSnapshot()
    {
        var storageDir = Path.Combine(
            Path.GetTempPath(),
            "asv-undo-test",
            Guid.NewGuid().ToString("N")
        );
        using var store = new MemoryPackUndoHistoryStore<string>(
            storageDir,
            inMemoryThresholdBytes: 128
        );

        var snapshot = (UndoSnapshot<string>)store.CreateSnapshot(["root", "child"], "change-1");
        var payload = Enumerable.Range(1, 32).Select(x => (byte)x).ToArray();
        store.SaveChange(new BinaryChange(payload));

        Assert.NotNull(snapshot.Data);
        Assert.Equal(payload, snapshot.Data);

        var loaded = new BinaryChange();
        store.LoadChange(snapshot.DataRefId, loaded);
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
        using var store = new MemoryPackUndoHistoryStore<string>(
            storageDir,
            inMemoryThresholdBytes: 16
        );

        var snapshot = (UndoSnapshot<string>)store.CreateSnapshot(["root"], "change-2");
        var payload = Enumerable.Range(1, 256).Select(x => (byte)x).ToArray();
        store.SaveChange(new BinaryChange(payload));

        var filePath = Path.Combine(storageDir, $"{snapshot.DataRefId:N}.undo");
        Assert.Null(snapshot.Data);
        Assert.True(File.Exists(filePath));

        var loaded = new BinaryChange();
        store.LoadChange(snapshot.DataRefId, loaded);
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
            var store = new MemoryPackUndoHistoryStore<string>(
                storageDir,
                inMemoryThresholdBytes: 128
            )
        )
        {
            var undoSnapshot = (UndoSnapshot<string>)store.CreateSnapshot(["u1"], "undo-change");
            store.SaveChange(new BinaryChange([1, 2, 3]));
            store.SaveUndoStack([undoSnapshot]);

            var redoSnapshot = (UndoSnapshot<string>)store.CreateSnapshot(["r1"], "redo-change");
            store.SaveChange(new BinaryChange([4, 5, 6]));
            store.SaveRedoStack([redoSnapshot]);
        }

        var undoJsonl = Path.Combine(storageDir, "undo-stack.jsonl");
        var redoJsonl = Path.Combine(storageDir, "redo-stack.jsonl");
        Assert.True(File.Exists(undoJsonl));
        Assert.True(File.Exists(redoJsonl));
        Assert.NotEmpty(File.ReadAllLines(undoJsonl).Where(x => !string.IsNullOrWhiteSpace(x)));
        Assert.NotEmpty(File.ReadAllLines(redoJsonl).Where(x => !string.IsNullOrWhiteSpace(x)));

        using var reloadedStore = new MemoryPackUndoHistoryStore<string>(
            storageDir,
            inMemoryThresholdBytes: 128
        );
        var undo = reloadedStore.LoadUndoStack().Cast<UndoSnapshot<string>>().ToArray();
        var redo = reloadedStore.LoadRedoStack().Cast<UndoSnapshot<string>>().ToArray();

        Assert.Single(undo);
        Assert.Single(redo);
        Assert.Equal("undo-change", undo[0].ChangeId);
        Assert.Equal("redo-change", redo[0].ChangeId);

        var loadedUndo = new BinaryChange();
        reloadedStore.LoadChange(undo[0].DataRefId, loadedUndo);
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
            writer.Write(Data);
        }

        public void Deserialize(ReadOnlySequence<byte> data)
        {
            Data = data.ToArray();
        }
    }
}
