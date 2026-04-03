using System.IO.Packaging;
using Asv.IO;
using Asv.XUnit;
using DotNext;
using JetBrains.Annotations;
using MessagePack;

namespace Asv.Store.Test;

[TestSubject(typeof(MessagePackArrayAsvPackagePart<>))]
public class MessagePackArrayAsvPackagePartTest(ITestOutputHelper log)
{
    private static readonly Uri PartUri = new("/tables/data.msgpack", UriKind.Relative);
    private const string ContentType = "application/msgpack+array";
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
        var logger = new TestLogger(log, TimeProvider.System, "MessagePackArrayAsvPackagePartTest");

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
            var part = new MessagePackArrayAsvPackagePart<TestRow>(
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
            var part = new MessagePackArrayAsvPackagePart<TestRow>(
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
        var logger = new TestLogger(log, TimeProvider.System, "MessagePackArrayAsvPackagePartTest");

        var first = new[] { new TestRow(1, "first", true), new TestRow(2, "second", false) };
        var second = new[] { new TestRow(10, "override", false) };

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new MessagePackArrayAsvPackagePart<TestRow>(
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
            var part = new MessagePackArrayAsvPackagePart<TestRow>(
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

    [MessagePackObject]
    public sealed record TestRow(
        [property: Key(0)] int Id,
        [property: Key(1)] string Name,
        [property: Key(2)] bool IsActive
    );
}
