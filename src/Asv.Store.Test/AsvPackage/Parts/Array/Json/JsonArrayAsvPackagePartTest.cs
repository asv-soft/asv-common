using System.IO.Packaging;
using System.Text;
using Asv.IO;
using Asv.XUnit;
using DotNext;
using JetBrains.Annotations;
using Xunit.Abstractions;

namespace Asv.Store.Test;

[TestSubject(typeof(JsonArrayAsvPackagePart<>))]
public class JsonArrayAsvPackagePartTest(ITestOutputHelper log)
{
    private static readonly Uri PartUri = new("/tables/data.jsonl", UriKind.Relative);
    private const string ContentType = "application/jsonl";
    private const string AllowedCharsName =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_ .";
    private const int NameSize = 32;

    [Theory]
    [InlineData(0, CompressionOption.NotCompressed)]
    [InlineData(1, CompressionOption.NotCompressed)]
    [InlineData(10, CompressionOption.NotCompressed)]
    [InlineData(100, CompressionOption.NotCompressed)]
    [InlineData(1000, CompressionOption.NotCompressed)]
    [InlineData(1000, CompressionOption.SuperFast)]
    [InlineData(1000, CompressionOption.Fast)]
    [InlineData(1000, CompressionOption.Normal)]
    [InlineData(1000, CompressionOption.Maximum)]
    public async Task WriteRead_Roundtrip_Works(int count, CompressionOption compression)
    {
        using var ms = new MemoryStream();
        var logger = new TestLogger(log, TimeProvider.System, "JsonArrayAsvPackagePartTest");

        var data = Enumerable
            .Range(0, count)
            .Select(i => new TestRow(
                i,
                Random.Shared.NextString(AllowedCharsName, NameSize),
                i % 2 == 0
            ))
            .ToArray();

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new JsonArrayAsvPackagePart<TestRow>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType,
                compression: compression
            );

            await part.Write(data, CancellationToken.None);
            part.Dispose();
        }

        log.WriteLine($"Saved {count} rows, package size: {ms.Length:N} bytes");

        ms.Position = 0;
        using (var pkg = Package.Open(ms, FileMode.Open, FileAccess.Read))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new JsonArrayAsvPackagePart<TestRow>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType
            );

            var actual = new List<TestRow>();
            await part.Read(actual.Add, CancellationToken.None);

            Assert.Equal(data, actual);
        }
    }

    [Fact]
    public async Task Write_Twice_OverwritesPart()
    {
        using var ms = new MemoryStream();
        var logger = new TestLogger(log, TimeProvider.System, "JsonArrayAsvPackagePartTest");

        var first = new[] { new TestRow(1, "first", true), new TestRow(2, "second", false) };
        var second = new[] { new TestRow(10, "override", false) };

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new JsonArrayAsvPackagePart<TestRow>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType
            );

            await part.Write(first, CancellationToken.None);
            await part.Write(second, CancellationToken.None);
            part.Dispose();
        }

        ms.Position = 0;
        using (var pkg = Package.Open(ms, FileMode.Open, FileAccess.Read))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new JsonArrayAsvPackagePart<TestRow>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType
            );

            var actual = new List<TestRow>();
            await part.Read(actual.Add, CancellationToken.None);

            Assert.Equal(second, actual);
        }
    }

    [Fact]
    public async Task Write_CreatesExpectedJsonlStructure()
    {
        using var ms = new MemoryStream();
        var logger = new TestLogger(log, TimeProvider.System, "JsonArrayAsvPackagePartTest");
        var data = new[] { new TestRow(1, "alpha", true), new TestRow(2, "beta", false) };

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new JsonArrayAsvPackagePart<TestRow>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType
            );

            await part.Write(data, CancellationToken.None);
            part.Dispose();
        }

        ms.Position = 0;
        using var pkgForRead = Package.Open(ms, FileMode.Open, FileAccess.Read);
        using var stream = pkgForRead.GetPart(PartUri).GetStream(FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var raw = await reader.ReadToEndAsync(CancellationToken.None);

        Assert.Contains("This file contains table data in JSONL format", raw);
        Assert.Contains("0000", raw);
        Assert.Contains("0001", raw);
        Assert.Contains("\"Name\": \"alpha\"", raw);
        Assert.Contains("\"Name\": \"beta\"", raw);
    }

    public sealed record TestRow(int Id, string Name, bool IsActive);
}
