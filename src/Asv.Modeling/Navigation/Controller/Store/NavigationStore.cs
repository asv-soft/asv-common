using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZLogger;

namespace Asv.Modeling;

public class NavigationStore : INavigationStore
{
    private const string ForwardFile = "navigation.forward.txt";
    private const string BackwardFile = "navigation.backward.txt";
    private readonly string _storageDirectory;
    private readonly ILogger _logger;
    
    public NavigationStore(string storageDirectory, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageDirectory);
        _storageDirectory = storageDirectory;
        _logger = logger ?? NullLogger.Instance;

        EnsureStorageDirectory();
    }

    public void Load(Action<NavPath> addForward, Action<NavPath> addBackward)
    {
        ArgumentNullException.ThrowIfNull(addForward);
        ArgumentNullException.ThrowIfNull(addBackward);

        LoadStack(GetForwardFilePath(), addForward);
        LoadStack(GetBackwardFilePath(), addBackward);
    }

    public void Save(IEnumerable<NavPath> forward, IEnumerable<NavPath> backward)
    {
        ArgumentNullException.ThrowIfNull(forward);
        ArgumentNullException.ThrowIfNull(backward);

        EnsureStorageDirectory();
        SaveStack(GetForwardFilePath(), forward);
        SaveStack(GetBackwardFilePath(), backward);
    }

    private void EnsureStorageDirectory()
    {
        if (Directory.Exists(_storageDirectory))
        {
            return;
        }

        _logger.ZLogDebug($"Create directory for navigation history: {_storageDirectory}");
        Directory.CreateDirectory(_storageDirectory);
    }

    private void LoadStack(string path, Action<NavPath> addItem)
    {
        if (File.Exists(path) == false)
        {
            return;
        }

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var navPath = NavPath.Parse(line);
                if (navPath.IsEmpty)
                {
                    continue;
                }

                addItem(navPath);
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"Skip invalid navigation history entry '{line}' from '{path}'");
            }
        }
    }

    private static void SaveStack(string path, IEnumerable<NavPath> items)
    {
        using var stream = File.Create(path);
        using var writer = new StreamWriter(stream);

        foreach (var item in items)
        {
            if (item.IsEmpty)
            {
                continue;
            }

            writer.WriteLine(item.ToString());
        }
    }

    private string GetForwardFilePath()
    {
        return Path.Combine(_storageDirectory, ForwardFile);
    }

    private string GetBackwardFilePath()
    {
        return Path.Combine(_storageDirectory, BackwardFile);
    }
}