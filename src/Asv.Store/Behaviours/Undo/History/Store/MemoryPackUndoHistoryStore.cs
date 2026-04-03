using System.Buffers;
using System.Text.Json;
using Asv.Common;

namespace Asv.Store.Undo.History.Store;

public class MemoryPackUndoHistoryStore<TId> : AsyncDisposableOnceBag, IUndoHistoryStore<TId>
{
    private const int DefaultInMemoryThresholdBytes = 1024;
    private const string UndoStackFileName = "undo-stack.jsonl";
    private const string RedoStackFileName = "redo-stack.jsonl";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly Lock _sync = new();
    private readonly Dictionary<Guid, UndoSnapshot<TId>> _snapshots = new();
    private readonly List<UndoSnapshot<TId>> _undoStack = new();
    private readonly List<UndoSnapshot<TId>> _redoStack = new();
    private readonly string _storageDirectory;
    private readonly int _inMemoryThresholdBytes;

    private Guid? _pendingSnapshotId;

    public MemoryPackUndoHistoryStore(
        string? storageDirectory = null,
        int inMemoryThresholdBytes = DefaultInMemoryThresholdBytes,
        bool deleteFilesOnDispose = true
    )
    {
        if (inMemoryThresholdBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inMemoryThresholdBytes));
        }

        _inMemoryThresholdBytes = inMemoryThresholdBytes;
        _ = deleteFilesOnDispose;
        _storageDirectory =
            storageDirectory
            ?? Path.Combine(Path.GetTempPath(), "asv-undo", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_storageDirectory);
    }

    public IEnumerable<IUndoSnapshot<TId>> LoadUndoStack()
    {
        lock (_sync)
        {
            _undoStack.Clear();
            _undoStack.AddRange(ReadStackFile(GetUndoStackFilePath()));
            RebuildSnapshotIndexUnsafe();
            return _undoStack.ToArray();
        }
    }

    public void SaveUndoStack(IEnumerable<IUndoSnapshot<TId>> snapshots)
    {
        if (snapshots == null)
        {
            throw new ArgumentNullException(nameof(snapshots));
        }

        lock (_sync)
        {
            _undoStack.Clear();
            foreach (var snapshot in snapshots)
            {
                _undoStack.Add(CloneSnapshot(snapshot));
            }

            RebuildSnapshotIndexUnsafe();
            WriteStackFile(GetUndoStackFilePath(), _undoStack);
        }
    }

    public IEnumerable<IUndoSnapshot<TId>> LoadRedoStack()
    {
        lock (_sync)
        {
            _redoStack.Clear();
            _redoStack.AddRange(ReadStackFile(GetRedoStackFilePath()));
            RebuildSnapshotIndexUnsafe();
            return _redoStack.ToArray();
        }
    }

    public void SaveRedoStack(IEnumerable<IUndoSnapshot<TId>> snapshots)
    {
        if (snapshots == null)
        {
            throw new ArgumentNullException(nameof(snapshots));
        }

        lock (_sync)
        {
            _redoStack.Clear();
            foreach (var snapshot in snapshots)
            {
                _redoStack.Add(CloneSnapshot(snapshot));
            }

            RebuildSnapshotIndexUnsafe();
            WriteStackFile(GetRedoStackFilePath(), _redoStack);
        }
    }

    public void LoadChange(Guid snapshotDataId, IChange item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        var snapshot = GetSnapshot(snapshotDataId);
        var data = snapshot.Data ?? File.ReadAllBytes(GetDataFilePath(snapshotDataId));
        item.Deserialize(new ReadOnlySequence<byte>(data));
    }

    public IUndoSnapshot<TId> CreateSnapshot(IEnumerable<TId> path, string changeId)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (string.IsNullOrWhiteSpace(changeId))
        {
            throw new ArgumentException(
                "Change id cannot be null or whitespace.",
                nameof(changeId)
            );
        }

        var snapshot = new UndoSnapshot<TId>
        {
            Path = path.ToArray(),
            ChangeId = changeId,
            DataRefId = Guid.NewGuid(),
            Data = null,
        };

        lock (_sync)
        {
            _snapshots[snapshot.DataRefId] = snapshot;
            _pendingSnapshotId = snapshot.DataRefId;
        }

        return snapshot;
    }

    public void SaveChange(IChange change)
    {
        if (change == null)
        {
            throw new ArgumentNullException(nameof(change));
        }

        var snapshot = GetPendingSnapshot();
        var writer = new ArrayBufferWriter<byte>();
        change.Serialize(writer);

        var size = writer.WrittenCount;
        var buffer = writer.WrittenMemory;

        if (size <= _inMemoryThresholdBytes)
        {
            snapshot.Data = buffer.ToArray();
        }
        else
        {
            var filePath = GetDataFilePath(snapshot.DataRefId);
            using var stream = File.Create(filePath);
            stream.Write(buffer.Span);
            snapshot.Data = null;
        }

        lock (_sync)
        {
            _pendingSnapshotId = null;
        }
    }

    private UndoSnapshot<TId> GetPendingSnapshot()
    {
        lock (_sync)
        {
            if (_pendingSnapshotId == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(CreateSnapshot)} must be called before {nameof(SaveChange)}."
                );
            }

            return GetSnapshot(_pendingSnapshotId.Value);
        }
    }

    private UndoSnapshot<TId> GetSnapshot(Guid id)
    {
        lock (_sync)
        {
            if (_snapshots.TryGetValue(id, out var snapshot))
            {
                return snapshot;
            }

            throw new KeyNotFoundException($"Snapshot '{id}' was not found.");
        }
    }

    private UndoSnapshot<TId> CloneSnapshot(IUndoSnapshot<TId> source)
    {
        var snapshot = source as UndoSnapshot<TId>;
        var clone = new UndoSnapshot<TId>
        {
            Path = source.Path.ToArray(),
            ChangeId = source.ChangeId,
            DataRefId = source.DataRefId,
            Data = snapshot?.Data is null ? null : snapshot.Data.ToArray(),
        };

        _snapshots[clone.DataRefId] = clone;
        return clone;
    }

    private void RebuildSnapshotIndexUnsafe()
    {
        _snapshots.Clear();
        foreach (var snapshot in _undoStack)
        {
            _snapshots[snapshot.DataRefId] = snapshot;
        }

        foreach (var snapshot in _redoStack)
        {
            _snapshots[snapshot.DataRefId] = snapshot;
        }
    }

    private string GetDataFilePath(Guid id)
    {
        return Path.Combine(_storageDirectory, $"{id:N}.undo");
    }

    private string GetUndoStackFilePath()
    {
        return Path.Combine(_storageDirectory, UndoStackFileName);
    }

    private string GetRedoStackFilePath()
    {
        return Path.Combine(_storageDirectory, RedoStackFileName);
    }

    private static List<UndoSnapshot<TId>> ReadStackFile(string path)
    {
        var result = new List<UndoSnapshot<TId>>();
        if (!File.Exists(path))
        {
            return result;
        }

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var snapshot = JsonSerializer.Deserialize<UndoSnapshot<TId>>(line, JsonOptions);
            if (snapshot != null)
            {
                result.Add(snapshot);
            }
        }

        return result;
    }

    private static void WriteStackFile(string path, IEnumerable<UndoSnapshot<TId>> snapshots)
    {
        using var stream = File.Create(path);
        using var writer = new StreamWriter(stream);
        foreach (var snapshot in snapshots)
        {
            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            writer.WriteLine(json);
        }
    }
}

public class UndoSnapshot<TId> : IUndoSnapshot<TId>
{
    public required IEnumerable<TId> Path { get; set; }
    public required string ChangeId { get; set; }
    public required Guid DataRefId { get; set; }
    public byte[]? Data { get; set; }
}
