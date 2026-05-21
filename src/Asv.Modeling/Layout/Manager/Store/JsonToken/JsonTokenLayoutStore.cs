using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using ZLogger;

namespace Asv.Modeling;

/// <summary>
/// Stores layout values in a single human-readable JSON file using in-memory <see cref="JToken"/> snapshots.
/// </summary>
/// <remarks>
/// Values are cached in memory. Calls to <see cref="Save{TData}"/> update the cache and mark the
/// store as dirty. Pending changes are written to disk by <see cref="Flush"/>, <see cref="Dispose"/>,
/// or the configured flush timer.
/// </remarks>
public sealed class JsonTokenLayoutStore : ILayoutStore
{
    private const string LayoutFileName = "layout.json";
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

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonTokenLayoutStore"/> class.
    /// </summary>
    /// <param name="storageDirectory">The directory that contains the layout file.</param>
    /// <param name="logger">The optional logger used for invalid data and flush errors.</param>
    /// <param name="flushInterval">The interval for automatic flushing. The default is five seconds.</param>
    /// <param name="timeProvider">The optional time provider used to create the flush timer.</param>
    /// <exception cref="ArgumentException"><paramref name="storageDirectory"/> is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="flushInterval"/> is less than or equal to zero.</exception>
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

    /// <inheritdoc />
    public bool TryLoad<TData>(NavPath path, string layoutId, out TData layoutData)
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

    /// <inheritdoc />
    public void Save<TData>(NavPath path, string layoutId, TData layoutData)
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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
            var token = JToken.Load(jsonReader);
            if (token.Type == JTokenType.Array)
            {
                var snapshots =
                    token.ToObject<List<JsonTokenLayoutSnapshot>>(_serializer)
                    ?? new List<JsonTokenLayoutSnapshot>();
                return ToSnapshotDictionary(snapshots);
            }

            var loaded =
                token.ToObject<Dictionary<string, JsonTokenLayoutSnapshot>>(_serializer)
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
                using (
                    var jsonWriter = new JsonTextWriter(writer)
                    {
                        Formatting = Formatting.Indented,
                        Indentation = 2,
                        IndentChar = ' ',
                    }
                )
                {
                    _serializer.Serialize(jsonWriter, GetOrderedSnapshots());
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

    private JsonTokenLayoutSnapshot[] GetOrderedSnapshots()
    {
        return _snapshots
            .Values.OrderBy(x => x.Path, StringComparer.Ordinal)
            .ThenBy(x => x.LayoutId, StringComparer.Ordinal)
            .ToArray();
    }

    private static Dictionary<string, JsonTokenLayoutSnapshot> ToSnapshotDictionary(
        IEnumerable<JsonTokenLayoutSnapshot> snapshots
    )
    {
        var result = new Dictionary<string, JsonTokenLayoutSnapshot>(StringComparer.Ordinal);
        foreach (var snapshot in snapshots)
        {
            if (
                string.IsNullOrWhiteSpace(snapshot.Path)
                || string.IsNullOrWhiteSpace(snapshot.LayoutId)
            )
            {
                continue;
            }

            result[GetLayoutKey(NavPath.Parse(snapshot.Path), snapshot.LayoutId)] = snapshot;
        }

        return result;
    }

    private static string GetLayoutKey(NavPath path, string layoutId)
    {
        var key = $"{path}\n{layoutId}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return $"{KeyPrefix}{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
