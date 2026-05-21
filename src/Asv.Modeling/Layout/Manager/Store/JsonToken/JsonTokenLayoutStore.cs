using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using ZLogger;

namespace Asv.Modeling;

public sealed class JsonTokenLayoutStore : ILayoutStore
{
    private const string LayoutFileName = "layout-token.json";
    private const string KeyPrefix = "layout_";
    private static readonly TimeSpan DefaultFlushInterval = TimeSpan.FromSeconds(5);
    private readonly Lock _sync = new();
    private readonly string _filePath;
    private readonly Dictionary<string, JsonTokenLayoutSnapshot> _snapshots;
    private readonly JsonSerializer _serializer;
    private readonly ILogger _logger;
    private readonly ITimer _flushTimer;
    private bool _dirty;
    private bool _disposed;

    public JsonTokenLayoutStore(
        string storageDirectory,
        ILogger? logger = null,
        TimeSpan? flushInterval = null,
        TimeProvider? timeProvider = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageDirectory);
        _logger = logger ?? NullLogger.Instance;
        _filePath = Path.Combine(storageDirectory, LayoutFileName);
        _serializer = JsonSerializer.CreateDefault();
        _serializer.Converters.Add(new StringEnumConverter());
        _snapshots = LoadSnapshots();

        var interval = flushInterval ?? DefaultFlushInterval;
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(flushInterval),
                flushInterval,
                "Flush interval must be positive."
            );
        }

        timeProvider ??= TimeProvider.System;
        _flushTimer = timeProvider.CreateTimer(FlushFromTimer, null, interval, interval);
    }

    public bool TryLoad<TData>(NavPath path, string layoutId, out TData layoutData)
        where TData : ILayoutData
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutId);

        using (_sync.EnterScope())
        {
            var key = GetLayoutKey(path, layoutId);
            if (_snapshots.TryGetValue(key, out var snapshot) == false)
            {
                layoutData = default!;
                return false;
            }

            try
            {
                if (snapshot.Path != path.ToString() || snapshot.LayoutId != layoutId)
                {
                    _logger.ZLogWarning(
                        $"Skip layout '{layoutId}' for '{path}': stored snapshot metadata does not match"
                    );
                    layoutData = default!;
                    return false;
                }

                var data = snapshot.Data.ToObject<TData>(_serializer);
                if (data == null)
                {
                    layoutData = default!;
                    return false;
                }

                layoutData = data;
                return true;
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"Skip invalid layout '{layoutId}' for '{path}'");
                layoutData = default!;
                return false;
            }
        }
    }

    public void Save<TData>(NavPath path, string layoutId, TData layoutData)
        where TData : ILayoutData
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutId);
        ArgumentNullException.ThrowIfNull(layoutData);

        using (_sync.EnterScope())
        {
            ThrowIfDisposed();

            var snapshot = new JsonTokenLayoutSnapshot
            {
                Path = path.ToString(),
                LayoutId = layoutId,
                Data = JToken.FromObject(layoutData, _serializer),
            };

            _snapshots[GetLayoutKey(path, layoutId)] = snapshot;
            _dirty = true;
        }
    }

    public void Flush()
    {
        using (_sync.EnterScope())
        {
            if (_disposed || _dirty == false)
            {
                return;
            }

            WriteSnapshotsToDisk();
            _dirty = false;
        }
    }

    public void Dispose()
    {
        using (_sync.EnterScope())
        {
            if (_disposed)
            {
                return;
            }

            _flushTimer.Dispose();
            if (_dirty)
            {
                WriteSnapshotsToDisk();
                _dirty = false;
            }

            _disposed = true;
        }
    }

    private void FlushFromTimer(object? state)
    {
        try
        {
            Flush();
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Error to flush layout store '{_filePath}'");
        }
    }

    private Dictionary<string, JsonTokenLayoutSnapshot> LoadSnapshots()
    {
        if (File.Exists(_filePath) == false)
        {
            return new Dictionary<string, JsonTokenLayoutSnapshot>(StringComparer.Ordinal);
        }

        try
        {
            using var stream = File.OpenRead(_filePath);
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader);
            var loaded =
                _serializer.Deserialize<Dictionary<string, JsonTokenLayoutSnapshot>>(jsonReader)
                ?? new Dictionary<string, JsonTokenLayoutSnapshot>(StringComparer.Ordinal);
            return new Dictionary<string, JsonTokenLayoutSnapshot>(loaded, StringComparer.Ordinal);
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"Skip invalid layout store '{_filePath}'");
            return new Dictionary<string, JsonTokenLayoutSnapshot>(StringComparer.Ordinal);
        }
    }

    private void WriteSnapshotsToDisk()
    {
        var directory = Path.GetDirectoryName(_filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory.CreateDirectory(directory);

        var tempFilePath = Path.Combine(
            directory,
            $"{Path.GetFileName(_filePath)}.{Guid.NewGuid():N}.tmp"
        );

        try
        {
            using (var stream = File.Create(tempFilePath, 4096, FileOptions.WriteThrough))
            {
                using (
                    var writer = new StreamWriter(
                        stream,
                        new UTF8Encoding(false),
                        bufferSize: 1024,
                        leaveOpen: true
                    )
                )
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    _serializer.Serialize(jsonWriter, _snapshots);
                    jsonWriter.Flush();
                }

                stream.Flush(true);
            }

            File.Move(tempFilePath, _filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }

    private static string GetLayoutKey(NavPath path, string layoutId)
    {
        var key = $"{path}\n{layoutId}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return $"{KeyPrefix}{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
