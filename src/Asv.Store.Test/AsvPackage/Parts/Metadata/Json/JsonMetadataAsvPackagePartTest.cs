using System.IO.Packaging;
using System.Text;
using Asv.IO;
using Asv.XUnit;
using DotNext;
using JetBrains.Annotations;
using Xunit.Abstractions;

namespace Asv.Store.Test;

[TestSubject(typeof(JsonMetadataAsvPackagePart<>))]
public class JsonMetadataAsvPackagePartTest(ITestOutputHelper log)
{
    private static readonly Uri PartUri = new("/meta/info.json", UriKind.Relative);
    private const string ContentType = "application/json";
    private const string AllowedCharsName =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_ .";
    private const int NameSize = 32;

    [Theory]
    [InlineData(CompressionOption.NotCompressed)]
    [InlineData(CompressionOption.SuperFast)]
    [InlineData(CompressionOption.Fast)]
    [InlineData(CompressionOption.Normal)]
    [InlineData(CompressionOption.Maximum)]
    public void WriteRead_Roundtrip_Works(CompressionOption compression)
    {
        using var ms = new MemoryStream();
        var logger = new TestLogger(log, TimeProvider.System, "JsonMetadataAsvPackagePartTest");
        var expected = CreateMetadata();

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new JsonMetadataAsvPackagePart<TestMetadata>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType,
                compression: compression
            );

            part.Write(expected);
            part.Dispose();
        }

        log.WriteLine($"Saved metadata, package size: {ms.Length:N} bytes");

        ms.Position = 0;
        using (var pkg = Package.Open(ms, FileMode.Open, FileAccess.Read))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new JsonMetadataAsvPackagePart<TestMetadata>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType
            );

            var actual = part.Read();
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void Write_Twice_OverwritesPart()
    {
        using var ms = new MemoryStream();
        var logger = new TestLogger(log, TimeProvider.System, "JsonMetadataAsvPackagePartTest");
        var first = new TestMetadata(1, "first", true, "a", "b");
        var second = new TestMetadata(10, "override", false, "x", "y");

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new JsonMetadataAsvPackagePart<TestMetadata>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType
            );

            part.Write(first);
            part.Write(second);
            part.Dispose();
        }

        ms.Position = 0;
        using (var pkg = Package.Open(ms, FileMode.Open, FileAccess.Read))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new JsonMetadataAsvPackagePart<TestMetadata>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType
            );

            var actual = part.Read();
            Assert.Equal(second, actual);
        }
    }

    [Fact]
    public void Write_Null_RemovesPart_AndReadReturnsNull()
    {
        using var ms = new MemoryStream();
        var logger = new TestLogger(log, TimeProvider.System, "JsonMetadataAsvPackagePartTest");

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new JsonMetadataAsvPackagePart<TestMetadata>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType
            );

            part.Write(CreateMetadata());
            part.Write(null);

            Assert.False(pkg.PartExists(PartUri));
            part.Dispose();
        }

        ms.Position = 0;
        using (var pkg = Package.Open(ms, FileMode.Open, FileAccess.Read))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new JsonMetadataAsvPackagePart<TestMetadata>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType
            );

            Assert.Null(part.Read());
        }
    }

    [Fact]
    public void Write_CreatesExpectedJson()
    {
        using var ms = new MemoryStream();
        var logger = new TestLogger(log, TimeProvider.System, "JsonMetadataAsvPackagePartTest");
        var expected = new TestMetadata(7, "alpha", true, "one", "two");

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new JsonMetadataAsvPackagePart<TestMetadata>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType
            );

            part.Write(expected);
            part.Dispose();
        }

        ms.Position = 0;
        using var pkgForRead = Package.Open(ms, FileMode.Open, FileAccess.Read);
        using var stream = pkgForRead.GetPart(PartUri).GetStream(FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false
        );
        var raw = reader.ReadToEnd();

        Assert.Contains("\"Id\": 7", raw);
        Assert.Contains("\"Name\": \"alpha\"", raw);
        Assert.Contains("\"IsActive\": true", raw);
        Assert.Contains("\"PrimaryTag\": \"one\"", raw);
        Assert.Contains("\"SecondaryTag\": \"two\"", raw);
    }

    private static TestMetadata CreateMetadata()
    {
        return new TestMetadata(
            Random.Shared.Next(),
            Random.Shared.NextString(AllowedCharsName, NameSize),
            Random.Shared.Next(0, 2) == 0,
            Random.Shared.NextString(AllowedCharsName, 8),
            Random.Shared.NextString(AllowedCharsName, 8)
        );
    }

    public sealed record TestMetadata(
        int Id,
        string Name,
        bool IsActive,
        string PrimaryTag,
        string SecondaryTag
    );
}
