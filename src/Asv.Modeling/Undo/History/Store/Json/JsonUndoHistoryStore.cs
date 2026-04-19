using System.Buffers;
using System.Text.Json;
using Asv.Common;
using DotNext.Buffers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZLogger;

namespace Asv.Modeling;

public class JsonUndoHistoryStore<TId> : AsyncDisposableOnceBag, IUndoHistoryStore
{
    private const int DefaultInMemoryThresholdBytes = 4 * 1024;
    private const string UndoStackFileName = "undo-stack.jsonl";
    private const string RedoStackFileName = "redo-stack.jsonl";
    private const string DataFileName = ".undo";
    private readonly string _storageDirectory;
    private readonly int _inMemoryThresholdBytes;
    private readonly ILogger _logger;

    public JsonUndoHistoryStore(
        string storageDirectory,
        ILogger? logger = null,
        int inMemoryThresholdBytes = DefaultInMemoryThresholdBytes
    )
    {
        if (inMemoryThresholdBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inMemoryThresholdBytes));
        }
        _storageDirectory = storageDirectory;
        _inMemoryThresholdBytes = inMemoryThresholdBytes;
        _logger = logger ?? NullLogger.Instance;
        
        if (Directory.Exists(_storageDirectory) == false)
        {
            _logger.ZLogDebug($"Create directory for undo history: {_storageDirectory}");
            Directory.CreateDirectory(_storageDirectory);
        }
    }

    public void LoadChange(IUndoSnapshot snapshot, IChange change)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(change);
        if (snapshot is not UndoSnapshot undoSnapshot)
        {
            throw new ArgumentException("Snapshot must be of type UndoSnapshot<TId>.");
        }

        if (undoSnapshot.Data != null)
        {
            var data = undoSnapshot.Data;
            change.Deserialize(new ReadOnlySequence<byte>(data));
        }
        else
        {
            using var fs = File.OpenRead(GetDataFilePath(undoSnapshot.DataRefId));
            var array = ArrayPool<byte>.Shared.Rent((int)fs.Length);
            try
            {
                fs.ReadExactly(array, 0, (int)fs.Length);
                change.Deserialize(new ReadOnlySequence<byte>(array));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array);
            }
        }
    }

    public void SaveChange(IUndoSnapshot snapshot, IChange change)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(change);
        if (snapshot is not UndoSnapshot undoSnapshot)
        {
            throw new ArgumentException("Snapshot must be of type UndoSnapshot<TId>.");
        }

        using var writer = new PoolingArrayBufferWriter<byte>();
        change.Serialize(writer);
        if (writer.WrittenCount > _inMemoryThresholdBytes)
        {
            var filePath = GetDataFilePath(undoSnapshot.DataRefId);
            using var stream = File.Create(filePath);
            stream.Write(writer.WrittenMemory.Span);
            undoSnapshot.Data = null;
        }
        else
        {
            undoSnapshot.Data = writer.WrittenMemory.ToArray();
        }
    }

    public void LoadUndoRedo(Action<IUndoSnapshot> addUndo, Action<IUndoSnapshot> addRedo)
    {
        var dataIndex = new HashSet<Ulid>();
        foreach (var undo in ReadStackFile(GetUndoStackFilePath()))
        {
            addUndo(undo);
            if (undo.Data == null)
            {
                dataIndex.Add(undo.DataRefId);
            }
        }
        foreach (var redo in ReadStackFile(GetRedoStackFilePath()))
        {
            addRedo(redo);
            if (redo.Data == null)
            {
                dataIndex.Add(redo.DataRefId);
            }
        }

        Directory
            .EnumerateFiles(_storageDirectory, $"*{DataFileName}")
            .Where(x => !dataIndex.Contains(Ulid.Parse(Path.GetFileNameWithoutExtension(x))))
            .ForEach(File.Delete);
    }

    public void SaveUndoRedo(
        IEnumerable<IUndoSnapshot> undo,
        IEnumerable<IUndoSnapshot> redo
    )
    {
        WriteStackFile(GetUndoStackFilePath(), undo.Cast<UndoSnapshot>());
        WriteStackFile(GetRedoStackFilePath(), redo.Cast<UndoSnapshot>());
    }

    public IUndoSnapshot CreateSnapshot(NavPath path, string changeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(changeId);
        return new UndoSnapshot
        {
            Path = path,
            ChangeId = changeId,
            DataRefId = Ulid.NewUlid(),
            Data = null,
        };
    }

    private string GetDataFilePath(Ulid id)
    {
        return Path.Combine(_storageDirectory, $"{id}{DataFileName}");
    }

    private string GetUndoStackFilePath()
    {
        return Path.Combine(_storageDirectory, UndoStackFileName);
    }

    private string GetRedoStackFilePath()
    {
        return Path.Combine(_storageDirectory, RedoStackFileName);
    }

    private IEnumerable<UndoSnapshot> ReadStackFile(string path)
    {
        if (File.Exists(path) == false)
        {
            yield break;
        }

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var snapshot = JsonSerializer.Deserialize(
                line,
                JsonUndoSnapshotJsonContext.Default.JsonUndoSnapshot
            );
            if (snapshot == null)
            {
                continue;
            }

            yield return new UndoSnapshot
            {
                Path = NavPath.Parse(snapshot.Path),
                ChangeId = snapshot.ChangeId,
                DataRefId = Ulid.Parse(snapshot.DataRefId),
                Data = string.IsNullOrEmpty(snapshot.Base64)
                    ? null
                    : Convert.FromBase64String(snapshot.Base64),
            };
        }
    }

    private void WriteStackFile(string path, IEnumerable<UndoSnapshot> snapshots)
    {
        using var stream = File.Create(path);
        using var writer = new StreamWriter(stream);

        foreach (var snapshot in snapshots)
        {
            
            var jsonSnapshot = new JsonUndoSnapshot
            {
                Path = snapshot.Path.ToString(),
                ChangeId = snapshot.ChangeId,
                DataRefId = snapshot.DataRefId.ToString(),
                Base64 =
                    snapshot.Data == null ? string.Empty : Convert.ToBase64String(snapshot.Data),
            };

            writer.WriteLine(
                JsonSerializer.Serialize(
                    jsonSnapshot,
                    JsonUndoSnapshotJsonContext.Default.JsonUndoSnapshot
                )
            );
        }
    }
}
