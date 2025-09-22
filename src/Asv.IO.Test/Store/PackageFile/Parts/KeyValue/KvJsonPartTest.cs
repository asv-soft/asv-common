using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Threading;
using Asv.Cfg.Test;
using DotNext;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test;

[TestSubject(typeof(KvJsonPart))]
public class KvJsonPartTest(ITestOutputHelper log)
{
    private static readonly Uri PartUri = new Uri("/meta/kvs.json", UriKind.Relative);
    private const string ContentType = "application/json";
    private const string AllowedCharsKey =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.";
    private const int keySize = 100;
    private const string AllowedCharsValue =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.!@#$%^&*()[]{};:,.<>?/|\\+=`~ \"'";
    private const int ValueSize = 500;

    [Theory]
    [InlineData(0, CompressionOption.NotCompressed)]
    [InlineData(1, CompressionOption.NotCompressed)]
    [InlineData(10, CompressionOption.NotCompressed)]
    [InlineData(100, CompressionOption.NotCompressed)]
    [InlineData(1000, CompressionOption.NotCompressed)]
    [InlineData(10000, CompressionOption.NotCompressed)]
    [InlineData(10000, CompressionOption.SuperFast)]
    [InlineData(10000, CompressionOption.Fast)]
    [InlineData(10000, CompressionOption.Normal)]
    [InlineData(10000, CompressionOption.Maximum)]
    public void SaveLoad_Roundtrip_Works(int count, CompressionOption compression)
    {
        var ms = new MemoryStream();
        var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
        var logger = new TestLogger(log, TimeProvider.System, "AsvFilePartTest");
        var ctx = new AsvFileContext(new Lock(), pkg, logger);
        var part = new KvJsonPart(PartUri, ContentType, compression, ctx);

        var data = new KeyValuePair<string, string>[count];
        var size = 0;
        for (int i = 0; i < count; i++)
        {
            data[i] = new KeyValuePair<string, string>(
                Random.Shared.NextString(AllowedCharsKey, keySize),
                Random.Shared.NextString(AllowedCharsValue, ValueSize)
            );
            size += data[i].Key.Length + data[i].Value.Length;
        }

        part.Save(data);
        part.Dispose();
        pkg.Close();
        log.WriteLine(
            $"Saved {count} items, total size in package: {ms.Length:N} bytes (approx. {size:N} bytes raw data)"
        );

        // Reopen package for reading
        ms.Position = 0;
        pkg = Package.Open(ms, FileMode.Open, FileAccess.Read);
        ctx = new AsvFileContext(new Lock(), pkg, logger);
        part = new KvJsonPart(PartUri, ContentType, CompressionOption.Maximum, ctx);

        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        part.Load(kv => dict[kv.Key] = kv.Value);

        Assert.Equal(data.Length, dict.Count);
        foreach (var kv in data)
        {
            Assert.True(dict.TryGetValue(kv.Key, out var v));
            Assert.Equal(kv.Value, v);
        }
    }

    [Fact]
    public void Save_Twice_OverwritesAndWarns()
    {
        var ms = new MemoryStream();
        var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
        var logger = new TestLogger(log, TimeProvider.System, "AsvFilePartTest");
        var ctx = new AsvFileContext(new Lock(), pkg, logger);
        var part = new KvJsonPart(PartUri, ContentType, CompressionOption.Maximum, ctx);

        part.Save(
            new[]
            {
                new KeyValuePair<string, string>("A", "1"),
                new KeyValuePair<string, string>("B", "2"),
            }
        );

        part.Save(
            new[]
            {
                new KeyValuePair<string, string>("A", "10"),
                new KeyValuePair<string, string>("C", "3"),
            }
        );
        part.Dispose();
        pkg.Close();

        // Reopen package for reading
        ms.Position = 0;
        pkg = Package.Open(ms, FileMode.Open, FileAccess.Read);
        ctx = new AsvFileContext(new Lock(), pkg, logger);
        part = new KvJsonPart(PartUri, ContentType, CompressionOption.Maximum, ctx);

        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        part.Load(kv => dict[kv.Key] = kv.Value);

        // Должны видеть перезаписанную картину
        Assert.Equal(2, dict.Count);
        Assert.Equal("10", dict["A"]);
        Assert.Equal("3", dict["C"]);
    }
}
