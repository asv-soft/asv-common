using System;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Linq;
using Asv.Common;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Asv.Cfg.Test;

public class ConfigurationKeyComparerTest
{
    [Theory]
    [InlineData("memory")]
    [InlineData("folder")]
    [InlineData("file")]
    [InlineData("package")]
    [InlineData("zip")]
    [InlineData("versioned")]
    public void MixedCaseLifecycle_AllBackends_UsesSingleKey(string backend)
    {
        using var fixture = new Backend(backend);
        using (var configuration = fixture.Open())
        {
            configuration.Set("Host", "first");
            Assert.True(configuration.Exist("hOsT"));
            Assert.Equal("first", configuration.Get("HOST", "fallback"));
            configuration.Set("host", "updated");
            Assert.Equal("updated", configuration.Get("Host", "fallback"));
            Assert.Equal("Host", Assert.Single(configuration.AvailableParts, x =>
                ConfigurationMixin.DefaultKeyComparer.Equals(x, "host")));
            configuration.Remove("HOST");
            Assert.False(configuration.Exist("Host"));
            Assert.DoesNotContain(configuration.AvailableParts, x =>
                ConfigurationMixin.DefaultKeyComparer.Equals(x, "host"));
            Assert.Equal("default", configuration.Get("RetryCount", "default"));
            Assert.Equal("default", configuration.Get("retrycount", "wrong"));
        }
        if (backend != "memory")
        {
            using var reopened = fixture.Open();
            Assert.False(reopened.Exist("HOST"));
            Assert.Equal("default", reopened.Get("RETRYCOUNT", "wrong"));
        }
    }

    [Theory]
    [InlineData("folder")]
    [InlineData("file")]
    [InlineData("package")]
    [InlineData("zip")]
    [InlineData("versioned")]
    public void UpdateExistingKey_PreservesStoredSpellingAndOtherValues(string backend)
    {
        using var fixture = new Backend(backend);
        using (var configuration = fixture.Open())
        {
            configuration.Set("Host", "old");
            configuration.Set("Port", 7341);
        }
        using (var configuration = fixture.Open())
        {
            configuration.Set("HOST", "new");
        }
        using (var configuration = fixture.Open())
        {
            Assert.Equal("new", configuration.Get("host", "wrong"));
            Assert.Equal(7341, configuration.Get("port", 0));
            Assert.Contains("Host", configuration.AvailableParts);
            Assert.DoesNotContain("HOST", configuration.AvailableParts);
        }
        Assert.Contains("Host", fixture.StoredNames());
        Assert.DoesNotContain("HOST", fixture.StoredNames());
    }

    [Theory]
    [InlineData("zip", "exist")]
    [InlineData("zip", "get")]
    [InlineData("zip", "set")]
    [InlineData("zip", "remove")]
    [InlineData("zip", "list")]
    [InlineData("versioned", "exist")]
    [InlineData("versioned", "get")]
    [InlineData("versioned", "set")]
    [InlineData("versioned", "remove")]
    [InlineData("versioned", "list")]
    public void DuplicateZipKeys_RejectOperationWithoutChangingValues(string backend, string operation)
    {
        using var fixture = new Backend(backend);
        using (fixture.Open()) { }
        using (var archive = new ZipArchive(fixture.Stream, ZipArchiveMode.Update, true))
        {
            WriteEntry(archive, "Host.json", "1");
            WriteEntry(archive, "host.json", "2");
        }
        using (var configuration = fixture.Open())
        {
            Assert.Throws<ConfigurationException>(() => Operate(configuration, operation));
        }
        using var result = new ZipArchive(fixture.Stream, ZipArchiveMode.Read, true);
        Assert.Equal("1", ReadEntry(result, "Host.json"));
        Assert.Equal("2", ReadEntry(result, "host.json"));
    }

    [Theory]
    [InlineData("file", "host")]
    [InlineData("package", "host")]
    [InlineData("file", "Host")]
    [InlineData("package", "Host")]
    public void DuplicateJsonKeys_RejectLoadWithoutOverwritingSource(string backend, string secondKey)
    {
        using var fixture = new Backend(backend);
        var json = "{\"Host\":1,\"" + secondKey + "\":2}";
        fixture.SeedJson(json);
        Assert.Throws<ConfigurationException>(() => fixture.Open());
        Assert.Equal(json, fixture.ReadJson());
    }

    [Theory]
    [InlineData("exist")]
    [InlineData("get")]
    [InlineData("set")]
    [InlineData("remove")]
    [InlineData("list")]
    public void DuplicateFolderKeys_RejectOperationWithoutChangingFiles(string operation)
    {
        var directory = Path.Combine(Path.GetTempPath(), "asv-cfg-conflict-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);

        // Invariant comparison ignores the zero-width character, while file names remain distinct.
        var first = Path.Combine(directory, "Host.json");
        var second = Path.Combine(directory, "Ho\u200bst.json");
        try
        {
            Assert.True(ConfigurationMixin.DefaultKeyComparer.Equals("Host", "Ho\u200bst"));
            File.WriteAllText(first, "1");
            File.WriteAllText(second, "2");
            using var configuration = new JsonConfiguration(directory);
            Assert.Throws<ConfigurationException>(() => Operate(configuration, operation));
            Assert.Equal("1", File.ReadAllText(first));
            Assert.Equal("2", File.ReadAllText(second));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void VersionMetadata_MixedCaseName_IsReadAndPreserved()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            WriteEntry(archive, "fileinfo.json", "{\"FileVersion\":\"1.0.0\",\"FileType\":\"Settings\"}");
        }
        using var configuration = new ZipJsonVersionedFile(stream, new SemVersion(1, 0, 0), "Settings", false, true);
        Assert.Equal(new SemVersion(1, 0, 0), configuration.FileVersion);
        Assert.Equal("fileinfo", Assert.Single(configuration.AvailableParts));
        Assert.True(configuration.Exist("FILEINFO"));
    }

    [Theory]
    [InlineData("memory")]
    [InlineData("folder")]
    [InlineData("file")]
    [InlineData("package")]
    [InlineData("zip")]
    [InlineData("versioned")]
    public void UnicodeEquivalentKeys_UseDefaultComparer(string backend)
    {
        using var fixture = new Backend(backend);
        using var configuration = fixture.Open();
        const string original = "État";
        const string equivalent = "e\u0301tat";
        Assert.True(ConfigurationMixin.DefaultKeyComparer.Equals(original, equivalent));
        configuration.Set(original, 1);
        Assert.Equal(1, configuration.Get(equivalent, 0));
        configuration.Set(equivalent, 2);
        Assert.Equal(2, configuration.Get(original, 0));
        Assert.Single(configuration.AvailableParts, key =>
            ConfigurationMixin.DefaultKeyComparer.Equals(key, original));
        configuration.Remove(equivalent);
        Assert.False(configuration.Exist(original));
    }

    [Fact]
    public void DuplicateVersionMetadata_RejectsOpenAndPreservesEntries()
    {
        using var stream = new MemoryStream();
        const string json = "{\"FileVersion\":\"1.0.0\",\"FileType\":\"Settings\"}";
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            WriteEntry(archive, "FileInfo.json", json);
            WriteEntry(archive, "fileinfo.json", json);
        }
        Assert.Throws<ConfigurationException>(() => new ZipJsonVersionedFile(
            stream, new SemVersion(1, 0, 0), "Settings", false, true));
        using var result = new ZipArchive(stream, ZipArchiveMode.Read, true);
        Assert.Equal(2, result.Entries.Count);
        Assert.Equal(json, ReadEntry(result, "FileInfo.json"));
        Assert.Equal(json, ReadEntry(result, "fileinfo.json"));
    }

    private static void Operate(IConfiguration configuration, string operation)
    {
        switch (operation)
        {
            case "exist": configuration.Exist("HOST"); break;
            case "get": configuration.Get("HOST", 0); break;
            case "set": configuration.Set("HOST", 3); break;
            case "remove": configuration.Remove("HOST"); break;
            case "list": _ = configuration.AvailableParts.ToArray(); break;
            default: throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    private static void WriteEntry(ZipArchive archive, string name, string json)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name).Open());
        writer.Write(json);
    }

    private static string ReadEntry(ZipArchive archive, string name)
    {
        using var reader = new StreamReader(archive.GetEntry(name)!.Open());
        return reader.ReadToEnd();
    }

    private sealed class Backend : IDisposable
    {
        private readonly string _kind;
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "asv-cfg-keys-" + Guid.NewGuid());
        private Package? _package;
        public MemoryStream Stream { get; } = new();

        public Backend(string kind)
        {
            _kind = kind;
            Directory.CreateDirectory(_directory);
            if (kind == "package")
            {
                _package = Package.Open(Stream, FileMode.Create, FileAccess.ReadWrite);
            }
        }

        public IConfiguration Open() => _kind switch
        {
            "memory" => new InMemoryConfiguration(),
            "folder" => new JsonConfiguration(_directory),
            "file" => new JsonOneFileConfiguration(Path.Combine(_directory, "settings.json"), true, null),
            "package" => new JsonPackageConfiguration(_package!, new Uri("/settings.json", UriKind.Relative), "application/json", CompressionOption.Normal, null),
            "zip" => new ZipJsonConfiguration(Stream, true),
            "versioned" => new ZipJsonVersionedFile(Stream, new SemVersion(1, 0, 0), "Settings", true, true),
            _ => throw new ArgumentOutOfRangeException(),
        };

        public void SeedJson(string json)
        {
            if (_kind == "file")
            {
                File.WriteAllText(Path.Combine(_directory, "settings.json"), json);
            }
            else
            {
                var part = _package!.CreatePart(new Uri("/settings.json", UriKind.Relative), "application/json");
                using var writer = new StreamWriter(part.GetStream(FileMode.Create, FileAccess.Write));
                writer.Write(json);
            }
        }

        public string ReadJson()
        {
            if (_kind == "file")
            {
                return File.ReadAllText(Path.Combine(_directory, "settings.json"));
            }
            using var reader = new StreamReader(_package!.GetPart(new Uri("/settings.json", UriKind.Relative)).GetStream());
            return reader.ReadToEnd();
        }

        public string[] StoredNames()
        {
            if (_kind == "folder")
            {
                return Directory.GetFiles(_directory).Select(x => Path.GetFileNameWithoutExtension(x)!).ToArray();
            }
            if (_kind is "file" or "package")
            {
                return JObject.Parse(ReadJson()).Properties().Select(x => x.Name).ToArray();
            }
            using var archive = new ZipArchive(Stream, ZipArchiveMode.Read, true);
            return archive.Entries.Select(x => Path.GetFileNameWithoutExtension(x.Name)).ToArray();
        }

        public void Dispose()
        {
            _package?.Close();
            Stream.Dispose();
            Directory.Delete(_directory, true);
        }
    }
}
