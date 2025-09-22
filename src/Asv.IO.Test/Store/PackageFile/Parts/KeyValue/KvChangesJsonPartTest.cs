using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Threading;
using Asv.Cfg.Test;
using Asv.IO;
using DotNext;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test.PackageFile.Parts.KeyValue;

[TestSubject(typeof(KvChangesJsonPart))]
public class KvChangesJsonPartTest(ITestOutputHelper log)
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
        var part = new KvChangesJsonPart(PartUri, ContentType, compression, ctx);

        var data = new KeyValueChange<string, string>[count];
        var size = 0;
        for (int i = 0; i < count; i++)
        {
            data[i] = new KeyValueChange<string, string>(
                DateTime.Now,
                Random.Shared.NextString(AllowedCharsKey, keySize),
                Random.Shared.NextString(AllowedCharsValue, ValueSize),
                Random.Shared.NextString(AllowedCharsValue, ValueSize)
            );
            size += data[i].Key.Length + data[i].OldValue.Length + data[i].NewValue.Length + 8;
            part.Append(data[i]);
        }

        part.Dispose();
        pkg.Close();
        log.WriteLine(
            $"Saved {count} items, total size in package: {ms.Length:N} bytes (approx. {size:N} bytes raw data)"
        );

        // Reopen package for reading
        ms.Position = 0;
        pkg = Package.Open(ms, FileMode.Open, FileAccess.Read);
        ctx = new AsvFileContext(new Lock(), pkg, logger);
        part = new KvChangesJsonPart(PartUri, ContentType, CompressionOption.Maximum, ctx);

        var arr = new KeyValueChange<string, string>[count];
        var index = 0;
        part.Load(
            (in KeyValueChange<string, string> change) =>
            {
                arr[index++] = change;
            }
        );

        Assert.Equal(data.Length, arr.Length);
        for (int i = 0; i < data.Length; i++)
        {
            data[i].Should().BeEquivalentTo(arr[i]);
        }
    }
}
