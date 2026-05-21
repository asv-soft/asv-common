using Asv.Modeling;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using ConsoleAppFramework;

namespace Asv.Common.Shell;

public sealed class LayoutStoreBenchmarkCommand
{
    [Command("layout-store-benchmarks")]
    public void Run()
    {
        BenchmarkRunner.Run<LayoutStoreBenchmarks>();
    }
}

[SimpleJob(RunStrategy.Throughput, launchCount: 1, warmupCount: 1, iterationCount: 2)]
[MemoryDiagnoser]
public class LayoutStoreBenchmarks
{
    private const string LayoutId = "Value";
    private readonly List<NavPath> _paths = [];
    private string _seedDirectory = string.Empty;

    [Params(100)]
    public int EntryCount { get; set; }

    [Params(256)]
    public int PayloadSize { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _paths.Clear();
        for (var i = 0; i < EntryCount; i++)
        {
            _paths.Add(new NavPath(new NavId($"node-{i}")));
        }

        _seedDirectory = CreateTempDirectory("layout-store-seed");
        using var store = CreateStore(_seedDirectory);
        for (var i = 0; i < EntryCount; i++)
        {
            store.Save(_paths[i], LayoutId, new BenchmarkLayoutData(i, PayloadSize));
        }

        store.Flush();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        DeleteDirectory(_seedDirectory);
    }

    [Benchmark]
    public long SaveAndFlush()
    {
        var directory = CreateTempDirectory("layout-store-save");
        try
        {
            using var store = CreateStore(directory);
            for (var i = 0; i < EntryCount; i++)
            {
                store.Save(_paths[i], LayoutId, new BenchmarkLayoutData(i, PayloadSize));
            }

            store.Flush();
            return GetStoreFileSize(directory);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Benchmark]
    public int OpenAndLoadAll()
    {
        using var store = CreateStore(_seedDirectory);
        var checksum = 0;
        for (var i = 0; i < EntryCount; i++)
        {
            if (store.TryLoad<BenchmarkLayoutData>(_paths[i], LayoutId, out var data))
            {
                checksum += data.Value;
            }
        }

        return checksum;
    }

    [Benchmark]
    public long UpdateOneAndFlush()
    {
        var directory = CreateTempDirectory("layout-store-update");
        try
        {
            CopyStoreFile(_seedDirectory, directory);
            using var store = CreateStore(directory);
            store.Save(_paths[0], LayoutId, new BenchmarkLayoutData(EntryCount, PayloadSize));
            store.Flush();
            return GetStoreFileSize(directory);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    private ILayoutStore CreateStore(string directory)
    {
        return new JsonTokenLayoutStore(directory, flushInterval: TimeSpan.FromHours(1));
    }

    private long GetStoreFileSize(string directory)
    {
        return Directory
            .GetFiles(directory, GetStoreFileName())
            .Select(x => new FileInfo(x).Length)
            .Single();
    }

    private void CopyStoreFile(string sourceDirectory, string targetDirectory)
    {
        var fileName = GetStoreFileName();
        System.IO.File.Copy(
            Path.Combine(sourceDirectory, fileName),
            Path.Combine(targetDirectory, fileName),
            overwrite: true
        );
    }

    private string GetStoreFileName()
    {
        return "layout-token.json";
    }

    private static string CreateTempDirectory(string prefix)
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "Asv.Common.Shell",
            prefix,
            Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) == false && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

public sealed class BenchmarkLayoutData : ILayoutData
{
    public BenchmarkLayoutData() { }

    public BenchmarkLayoutData(int value, int payloadSize)
    {
        Value = value;
        Payload = new byte[payloadSize];
        for (var i = 0; i < Payload.Length; i++)
        {
            Payload[i] = unchecked((byte)(value + i));
        }
    }

    public int Value { get; set; }

    public byte[] Payload { get; set; } = [];
}
