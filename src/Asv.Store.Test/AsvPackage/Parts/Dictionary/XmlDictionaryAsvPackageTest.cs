using System.IO.Packaging;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Asv.XUnit;
using JetBrains.Annotations;

namespace Asv.Store.Test;

[TestSubject(typeof(XmlDictionaryAsvPackage))]
public class XmlDictionaryAsvPackageTest(ITestOutputHelper log)
{
    private static readonly Uri PartUri = new("/dictionary.xml", UriKind.Relative);
    private const string ContentType = "application/xml";
    private const string Description = "Crew report dictionary";
    private const double ExpectedDistance = 1564.65465;

    [Fact]
    public void Write_DoesNotSaveUntilFlush()
    {
        using var ms = new MemoryStream();
        using var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
        var part = CreatePart(pkg);

        part.Write(new TestPilotDto("P1", "John"), "report", "crew", "pilot");

        Assert.False(pkg.PartExists(PartUri));

        part.Flush();

        Assert.True(pkg.PartExists(PartUri));
    }

    [Fact]
    public void Dispose_SavesChangedDocument()
    {
        using var ms = new MemoryStream();

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var part = CreatePart(pkg);
            part.Write(new TestPilotDto("P1", "John"), "report", "crew", "pilot");
            part.Dispose();
        }

        ms.Position = 0;
        using var readPkg = Package.Open(ms, FileMode.Open, FileAccess.Read);
        var readPart = CreatePart(readPkg);

        var actual = readPart.Read<TestPilotDto>("report", "crew", "pilot");

        Assert.NotNull(actual);
        Assert.Equal("P1", actual.Id);
        Assert.Equal("John", actual.Name);
    }

    [Theory]
    [InlineData(CompressionOption.NotCompressed)]
    [InlineData(CompressionOption.SuperFast)]
    [InlineData(CompressionOption.Fast)]
    [InlineData(CompressionOption.Normal)]
    [InlineData(CompressionOption.Maximum)]
    public void WriteRead_DifferentPaths_RoundtripWorks(CompressionOption compression)
    {
        using var ms = new MemoryStream();
        var expectedPilot = new TestPilotDto("P1", "John");

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var part = CreatePart(pkg, compression);
            part.Write(expectedPilot, "report", "crew", "pilot");
            part.Write(ExpectedDistance, "report", "crew", "pilotDistance");
            part.Dispose();
        }

        log.WriteLine($"Saved dictionary, package size: {ms.Length:N} bytes");

        ms.Position = 0;
        using var readPkg = Package.Open(ms, FileMode.Open, FileAccess.Read);
        var readPart = CreatePart(readPkg);

        var actualPilot = readPart.Read<TestPilotDto>("report", "crew", "pilot");
        var actualDistance = readPart.Read<double>("report", "crew", "pilotDistance");

        Assert.NotNull(actualPilot);
        Assert.Equal(expectedPilot, actualPilot);
        Assert.Equal(ExpectedDistance, actualDistance);
    }

    [Fact]
    public void Write_SamePath_OverwritesPreviousValue()
    {
        using var ms = new MemoryStream();

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var part = CreatePart(pkg);
            part.Write(new TestPilotDto("P1", "John"), "report", "crew", "pilot");
            part.Write(ExpectedDistance, "report", "crew", "pilot");
            part.Dispose();
        }

        ms.Position = 0;
        using var readPkg = Package.Open(ms, FileMode.Open, FileAccess.Read);
        var readPart = CreatePart(readPkg);

        Assert.Null(readPart.Read<TestPilotDto>("report", "crew", "pilot"));
        Assert.Equal(ExpectedDistance, readPart.Read<double>("report", "crew", "pilot"));

        var packagePart = readPkg.GetPart(PartUri);
        using var stream = packagePart.GetStream(FileMode.Open, FileAccess.Read);
        var xml = XDocument.Load(stream);
        var pilots = xml.Descendants().Where(x => x.Name.LocalName == "pilot").ToArray();

        Assert.Single(pilots);
    }

    [Fact]
    public void EnumerablePath_Overloads_Work()
    {
        using var ms = new MemoryStream();
        using var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
        var part = CreatePart(pkg);
        var expected = new TestPilotDto("P1", "John");
        IEnumerable<string> path = new[] { "report", "crew", "pilot" };

        part.Write(expected, path);

        Assert.Equal(expected, part.Read<TestPilotDto>(path));

        part.Write<TestPilotDto>(null, path);

        Assert.Null(part.Read<TestPilotDto>(path));
    }

    [Fact]
    public void Read_DifferentDtoType_ReturnsDefault()
    {
        using var ms = new MemoryStream();

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var part = CreatePart(pkg);
            part.Write(new TestPilotDto("P1", "John"), "report", "crew", "pilot");
            part.Dispose();
        }

        ms.Position = 0;
        using var readPkg = Package.Open(ms, FileMode.Open, FileAccess.Read);
        var readPart = CreatePart(readPkg);

        Assert.Null(readPart.Read<OtherPilotDto>("report", "crew", "pilot"));
    }

    [Fact]
    public void WriteNull_TypedEntry_RemovesValue()
    {
        using var ms = new MemoryStream();

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var part = CreatePart(pkg);
            part.Write(new TestPilotDto("P1", "John"), "report", "crew", "pilot");
            part.Write(ExpectedDistance, "report", "crew", "pilotDistance");

            part.Write<double?>(null, "report", "crew", "pilotDistance");
            part.Dispose();
        }

        ms.Position = 0;
        using var readPkg = Package.Open(ms, FileMode.Open, FileAccess.Read);
        var readPart = CreatePart(readPkg);

        Assert.NotNull(readPart.Read<TestPilotDto>("report", "crew", "pilot"));
        Assert.Null(readPart.Read<double?>("report", "crew", "pilotDistance"));
    }

    [Fact]
    public void Write_PathSegmentsNamedLikeMetadata_Work()
    {
        using var ms = new MemoryStream();
        var expected = new TestPilotDto("P1", "John");

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var part = CreatePart(pkg);
            part.Write(expected, "report", "type", "assembly");
            part.Write(ExpectedDistance, "report", "type", "distance");
            part.Dispose();
        }

        ms.Position = 0;
        using var readPkg = Package.Open(ms, FileMode.Open, FileAccess.Read);
        var readPart = CreatePart(readPkg);

        Assert.Equal(expected, readPart.Read<TestPilotDto>("report", "type", "assembly"));
        Assert.Equal(ExpectedDistance, readPart.Read<double>("report", "type", "distance"));

        var packagePart = readPkg.GetPart(PartUri);
        using var stream = packagePart.GetStream(FileMode.Open, FileAccess.Read);
        var xml = XDocument.Load(stream);
        var reportNode = Assert.Single(xml.Root!.Elements(), x => x.Name.LocalName == "report");
        var typeNode = Assert.Single(reportNode.Elements(), x => x.Name.LocalName == "type");
        var assemblyNode = Assert.Single(
            typeNode.Elements(),
            x => x.Name.LocalName == "assembly"
        );

        Assert.Null(typeNode.Attribute("type"));
        Assert.Equal(typeof(TestPilotDto).FullName, assemblyNode.Attribute("type")?.Value);
        Assert.Equal(
            typeof(TestPilotDto).Assembly.GetName().Name,
            assemblyNode.Attribute("assembly")?.Value
        );
    }

    [Fact]
    public void Write_CreatesHumanReadableXmlWithTypeMarkers()
    {
        using var ms = new MemoryStream();

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var part = CreatePart(pkg, description: Description);
            part.Write(new TestPilotDto("P1", "John"), "report", "crew", "pilot");
            part.Write(ExpectedDistance, "report", "crew", "pilotDistance");
            part.Dispose();
        }

        ms.Position = 0;
        using var readPkg = Package.Open(ms, FileMode.Open, FileAccess.Read);
        var packagePart = readPkg.GetPart(PartUri);
        using var stream = packagePart.GetStream(FileMode.Open, FileAccess.Read);
        var xml = XDocument.Load(stream);

        log.WriteLine("Dictionary XML:");
        log.WriteLine(xml.ToString());

        var pilot = Assert.Single(xml.Descendants(), x => x.Name.LocalName == "pilot");
        var pilotDistance = Assert.Single(
            xml.Descendants(),
            x => x.Name.LocalName == "pilotDistance"
        );

        Assert.Equal(ContentType, packagePart.ContentType);
        Assert.Equal("dictionary", xml.Root?.Name.LocalName);
        Assert.Equal("Asv.Store.Dictionary", xml.Root?.Attribute("format")?.Value);
        Assert.Equal("1", xml.Root?.Attribute("version")?.Value);
        Assert.Equal(Description, xml.Root?.Attribute("description")?.Value);
        Assert.Equal("report", Assert.Single(xml.Root!.Elements()).Name.LocalName);
        Assert.Equal(typeof(TestPilotDto).FullName, pilot.Attribute("type")?.Value);
        Assert.Equal(
            typeof(TestPilotDto).Assembly.GetName().Name,
            pilot.Attribute("assembly")?.Value
        );
        Assert.Contains(pilot.Elements(), x => x.Name.LocalName == nameof(TestPilotDto));
        Assert.Equal(typeof(double).FullName, pilotDistance.Attribute("type")?.Value);
        Assert.Equal(
            typeof(double).Assembly.GetName().Name,
            pilotDistance.Attribute("assembly")?.Value
        );
        Assert.Contains(pilotDistance.Elements(), x => x.Name.LocalName == "double");
    }

    [Fact]
    public void Constructor_LegacyRoot_Throws()
    {
        using var ms = new MemoryStream();
        using var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
        var packagePart = pkg.CreatePart(PartUri, ContentType, CompressionOption.Maximum);
        using (var stream = packagePart.GetStream(FileMode.Create, FileAccess.Write))
        {
            new XDocument(new XElement("report")).Save(stream);
        }

        var ex = Assert.Throws<InvalidOperationException>(() => CreatePart(pkg));

        Assert.Contains("root must be 'dictionary'", ex.Message);
    }

    private XmlDictionaryAsvPackage CreatePart(
        Package pkg,
        CompressionOption compression = CompressionOption.Maximum,
        string description = ""
    )
    {
        var logger = new TestLogger(log, TimeProvider.System, nameof(XmlDictionaryAsvPackageTest));
        var ctx = new AsvPackageContext(new Lock(), pkg, logger);
        return new XmlDictionaryAsvPackage(
            PartUri,
            ctx,
            parent: null,
            contentType: ContentType,
            compression: compression,
            description: description
        );
    }

    [DataContract(Name = nameof(TestPilotDto))]
    public sealed record TestPilotDto
    {
        public TestPilotDto()
        {
            Id = string.Empty;
            Name = string.Empty;
        }

        public TestPilotDto(string id, string name)
        {
            Id = id;
            Name = name;
        }

        [DataMember(Order = 0)]
        public string Id { get; init; }

        [DataMember(Order = 1)]
        public string Name { get; init; }
    }

    [DataContract(Name = nameof(OtherPilotDto))]
    public sealed record OtherPilotDto
    {
        public OtherPilotDto()
        {
            Value = string.Empty;
        }

        [DataMember(Order = 0)]
        public string Value { get; init; }
    }
}
