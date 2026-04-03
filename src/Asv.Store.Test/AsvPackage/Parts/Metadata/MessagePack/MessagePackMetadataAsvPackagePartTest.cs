using System.IO.Packaging;
using Asv.IO;
using Asv.XUnit;
using DotNext;
using JetBrains.Annotations;
using MessagePack;


namespace Asv.Store.Test;

[TestSubject(typeof(MessagePackMetadataAsvPackagePart<>))]
public class MessagePackMetadataAsvPackagePartTest(ITestOutputHelper log)
{
    private static readonly Uri PartUri = new("/meta/info.msgpack", UriKind.Relative);
    private const string ContentType = "application/msgpack";
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
        var logger = new TestLogger(
            log,
            TimeProvider.System,
            "MessagePackMetadataAsvPackagePartTest"
        );
        var expected = CreateMetadata();

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new MessagePackMetadataAsvPackagePart<TestMetadata>(
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
            var part = new MessagePackMetadataAsvPackagePart<TestMetadata>(
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
        var logger = new TestLogger(
            log,
            TimeProvider.System,
            "MessagePackMetadataAsvPackagePartTest"
        );
        var first = new TestMetadata(1, "first", true, "a", "b");
        var second = new TestMetadata(10, "override", false, "x", "y");

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new MessagePackMetadataAsvPackagePart<TestMetadata>(
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
            var part = new MessagePackMetadataAsvPackagePart<TestMetadata>(
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
        var logger = new TestLogger(
            log,
            TimeProvider.System,
            "MessagePackMetadataAsvPackagePartTest"
        );

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new MessagePackMetadataAsvPackagePart<TestMetadata>(
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
            var part = new MessagePackMetadataAsvPackagePart<TestMetadata>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType
            );

            Assert.Null(part.Read());
        }
    }

    [Fact]
    public void Write_CreatesExpectedMessagePackPayload()
    {
        using var ms = new MemoryStream();
        var logger = new TestLogger(
            log,
            TimeProvider.System,
            "MessagePackMetadataAsvPackagePartTest"
        );
        var expected = new TestMetadata(7, "alpha", true, "one", "two");

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new MessagePackMetadataAsvPackagePart<TestMetadata>(
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
        var raw = MessagePackSerializer.Deserialize<TestMetadata>(stream);

        Assert.Equal(expected, raw);
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

    [MessagePackObject]
    public sealed record TestMetadata(
        [property: Key(0)] int Id,
        [property: Key(1)] string Name,
        [property: Key(2)] bool IsActive,
        [property: Key(3)] string PrimaryTag,
        [property: Key(4)] string SecondaryTag
    );
}
