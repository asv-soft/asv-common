using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Asv.Common;
using DotNext.Buffers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZLogger;

namespace Asv.Modeling;

public class JsonLayoutStore : ILayoutStore
{
    private const string LayoutFileExtension = ".layout.json";
    private readonly string _storageDirectory;
    private readonly ILogger _logger;

    public JsonLayoutStore(string storageDirectory, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageDirectory);
        _storageDirectory = storageDirectory;
        _logger = logger ?? NullLogger.Instance;

        EnsureStorageDirectory();
    }

    public bool Load(NavPath path, string layoutId, ILayoutData layoutData)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutId);
        ArgumentNullException.ThrowIfNull(layoutData);

        var filePath = GetLayoutFilePath(path, layoutId);
        if (File.Exists(filePath) == false)
        {
            return false;
        }

        try
        {
            var snapshot = JsonSerializer.Deserialize(
                File.ReadAllText(filePath),
                JsonLayoutSnapshotJsonContext.Default.JsonLayoutSnapshot
            );
            if (snapshot?.Base64 == null)
            {
                return false;
            }

            var data = Convert.FromBase64String(snapshot.Base64);
            layoutData.Deserialize(new ReadOnlySequence<byte>(data));
            return true;
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"Skip invalid layout '{layoutId}' from '{filePath}'");
            return false;
        }
    }

    public void Save(NavPath path, string layoutId, ILayoutData layoutData)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutId);
        ArgumentNullException.ThrowIfNull(layoutData);

        EnsureStorageDirectory();
        using var writer = new PoolingArrayBufferWriter<byte>();
        layoutData.Serialize(writer);

        var snapshot = new JsonLayoutSnapshot
        {
            Path = path.ToString(),
            LayoutId = layoutId,
            Base64 = Convert.ToBase64String(writer.WrittenMemory.Span),
        };

        File.WriteAllText(
            GetLayoutFilePath(path, layoutId),
            JsonSerializer.Serialize(
                snapshot,
                JsonLayoutSnapshotJsonContext.Default.JsonLayoutSnapshot
            )
        );
    }

    public void Dispose() { }

    private void EnsureStorageDirectory()
    {
        if (Directory.Exists(_storageDirectory))
        {
            return;
        }

        _logger.ZLogDebug($"Create directory for layout store: {_storageDirectory}");
        Directory.CreateDirectory(_storageDirectory);
    }

    private string GetLayoutFilePath(NavPath path, string layoutId)
    {
        var key = $"{path}\n{layoutId}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Path.Combine(
            _storageDirectory,
            $"{Convert.ToHexString(hash).ToLowerInvariant()}{LayoutFileExtension}"
        );
    }
}
