using System.IO.Packaging;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Asv.IO;
using Asv.XUnit;
using DotNext;
using JetBrains.Annotations;

namespace Asv.Store.Test;

[TestSubject(typeof(XmlMetadataAsvPackagePart<>))]
public class XmlMetadataAsvPackagePartTest(ITestOutputHelper log)
{
    private static readonly Uri PartUri = new("/meta/info.xml", UriKind.Relative);
    private const string ContentType = "application/xml";
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
        var logger = new TestLogger(log, TimeProvider.System, "XmlMetadataAsvPackagePartTest");
        var expected = CreateMetadata();

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new XmlMetadataAsvPackagePart<TestMetadata>(
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
            var part = new XmlMetadataAsvPackagePart<TestMetadata>(
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
        var logger = new TestLogger(log, TimeProvider.System, "XmlMetadataAsvPackagePartTest");
        var first = new TestMetadata(1, "first", true, "a", "b");
        var second = new TestMetadata(10, "override", false, "x", "y");

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new XmlMetadataAsvPackagePart<TestMetadata>(
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
            var part = new XmlMetadataAsvPackagePart<TestMetadata>(
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
        var logger = new TestLogger(log, TimeProvider.System, "XmlMetadataAsvPackagePartTest");

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new XmlMetadataAsvPackagePart<TestMetadata>(
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
            var part = new XmlMetadataAsvPackagePart<TestMetadata>(
                PartUri,
                ctx,
                parent: null,
                contentType: ContentType
            );

            Assert.Null(part.Read());
        }
    }

    [Fact]
    public void Write_CreatesExpectedXml()
    {
        using var ms = new MemoryStream();
        var logger = new TestLogger(log, TimeProvider.System, "XmlMetadataAsvPackagePartTest");
        var expected = new TestMetadata(7, "alpha", true, "one", "two");

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var ctx = new AsvPackageContext(new Lock(), pkg, logger);
            var part = new XmlMetadataAsvPackagePart<TestMetadata>(
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
        var packagePart = pkgForRead.GetPart(PartUri);
        using var stream = packagePart.GetStream(FileMode.Open, FileAccess.Read);
        var raw = XDocument.Load(stream);

        Assert.Equal(ContentType, packagePart.ContentType);
        Assert.Equal(nameof(TestMetadata), raw.Root?.Name.LocalName);
        AssertElementValue(raw, nameof(TestMetadata.Id), "7");
        AssertElementValue(raw, nameof(TestMetadata.Name), "alpha");
        AssertElementValue(raw, nameof(TestMetadata.IsActive), "true");
        AssertElementValue(raw, nameof(TestMetadata.PrimaryTag), "one");
        AssertElementValue(raw, nameof(TestMetadata.SecondaryTag), "two");
    }

    private static void AssertElementValue(XDocument document, string elementName, string expected)
    {
        var element = Assert.Single(document.Descendants(), x => x.Name.LocalName == elementName);
        Assert.Equal(expected, element.Value);
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

    [DataContract(Name = nameof(TestMetadata))]
    public sealed record TestMetadata
    {
        public TestMetadata()
        {
            Name = string.Empty;
            PrimaryTag = string.Empty;
            SecondaryTag = string.Empty;
        }

        public TestMetadata(
            int id,
            string name,
            bool isActive,
            string primaryTag,
            string secondaryTag
        )
        {
            Id = id;
            Name = name;
            IsActive = isActive;
            PrimaryTag = primaryTag;
            SecondaryTag = secondaryTag;
        }

        [DataMember(Order = 0)]
        public int Id { get; init; }

        [DataMember(Order = 1)]
        public string Name { get; init; }

        [DataMember(Order = 2)]
        public bool IsActive { get; init; }

        [DataMember(Order = 3)]
        public string PrimaryTag { get; init; }

        [DataMember(Order = 4)]
        public string SecondaryTag { get; init; }
    }
}
