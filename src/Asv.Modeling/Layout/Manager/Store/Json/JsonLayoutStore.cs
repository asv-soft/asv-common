using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZLogger;

namespace Asv.Modeling;

public class JsonLayoutStore : ILayoutStore
{
    private const string LayoutFileName = "layout.json";
    private const string KeyPrefix = "layout_";
    private readonly string _filePath;
    private readonly Dictionary<string, JsonLayoutSnapshot> _snapshots;
    private readonly ILogger _logger;

    public JsonLayoutStore(string storageDirectory, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageDirectory);
        _logger = logger ?? NullLogger.Instance;
        _filePath = Path.Combine(storageDirectory, LayoutFileName);
        _snapshots = LoadSnapshots();
    }

    public bool TryLoad<TData>(NavPath path, string layoutId, out TData layoutData)
        where TData : ILayoutData, new()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutId);

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

            var data = JsonSerializer.Deserialize<TData>(snapshot.Json);
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

    public void Save<TData>(NavPath path, string layoutId, TData layoutData)
        where TData : ILayoutData
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutId);
        if (layoutData is null)
        {
            throw new ArgumentNullException(nameof(layoutData));
        }

        var snapshot = new JsonLayoutSnapshot
        {
            Path = path.ToString(),
            LayoutId = layoutId,
            SchemaVersion = layoutData.SchemaVersion,
            Json = JsonSerializer.Serialize(layoutData),
        };

        _snapshots[GetLayoutKey(path, layoutId)] = snapshot;
        SaveSnapshots();
    }

    public void Dispose() { }

    private Dictionary<string, JsonLayoutSnapshot> LoadSnapshots()
    {
        if (File.Exists(_filePath) == false)
        {
            return new Dictionary<string, JsonLayoutSnapshot>(StringComparer.Ordinal);
        }

        try
        {
            using var stream = File.OpenRead(_filePath);
            return JsonSerializer.Deserialize(
                    stream,
                    JsonLayoutStoreJsonContext.Default.DictionaryStringJsonLayoutSnapshot
                ) ?? new Dictionary<string, JsonLayoutSnapshot>(StringComparer.Ordinal);
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"Skip invalid layout store '{_filePath}'");
            return new Dictionary<string, JsonLayoutSnapshot>(StringComparer.Ordinal);
        }
    }

    private void SaveSnapshots()
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
                JsonSerializer.Serialize(
                    stream,
                    _snapshots,
                    JsonLayoutStoreJsonContext.Default.DictionaryStringJsonLayoutSnapshot
                );
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

    private static string GetLayoutKey(NavPath path, string layoutId)
    {
        var key = $"{path}\n{layoutId}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return $"{KeyPrefix}{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}

[JsonSerializable(typeof(Dictionary<string, JsonLayoutSnapshot>))]
internal partial class JsonLayoutStoreJsonContext : JsonSerializerContext { }
