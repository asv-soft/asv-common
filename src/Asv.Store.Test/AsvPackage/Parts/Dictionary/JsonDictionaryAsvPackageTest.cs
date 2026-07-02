using System.IO.Packaging;
using System.Runtime.Serialization;
using Asv.XUnit;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

namespace Asv.Store.Test;

[TestSubject(typeof(JsonDictionaryAsvPackage))]
public class JsonDictionaryAsvPackageTest(ITestOutputHelper log)
{
    private static readonly Uri PartUri = new("/dictionary.json", UriKind.Relative);
    private const string ContentType = "application/json";
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

        var json = ReadJson(readPkg);
        var pilotProperties = json.SelectTokens("$..pilot").ToArray();

        Assert.Single(pilotProperties);
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
            part.Write(expected, "report", "type", "assembly", "value");
            part.Write(ExpectedDistance, "report", "type", "assembly", "distance");
            part.Dispose();
        }

        ms.Position = 0;
        using var readPkg = Package.Open(ms, FileMode.Open, FileAccess.Read);
        var readPart = CreatePart(readPkg);
        var json = ReadJson(readPkg);

        Assert.Equal(expected, readPart.Read<TestPilotDto>("report", "type", "assembly", "value"));
        Assert.Equal(
            ExpectedDistance,
            readPart.Read<double>("report", "type", "assembly", "distance")
        );
        var entries = Assert.IsType<JObject>(json["dictionary"]?["entries"]);
        Assert.NotNull(entries["report"]?["type"]?["assembly"]?["value"]?["$type"]);
        Assert.NotNull(entries["report"]?["type"]?["assembly"]?["distance"]?["$type"]);
    }

    [Theory]
    [InlineData("$type")]
    [InlineData("$assembly")]
    [InlineData("$value")]
    public void Write_PathSegmentNamedLikeMetadataProperty_Throws(string pathSegment)
    {
        using var ms = new MemoryStream();
        using var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
        var part = CreatePart(pkg);

        var ex = Assert.Throws<ArgumentException>(() =>
            part.Write(new TestPilotDto("P1", "John"), "report", pathSegment)
        );

        Assert.Contains("reserved", ex.Message);
    }

    [Fact]
    public void Write_CreatesHumanReadableJsonWithTypeMarkers()
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
        var json = ReadJson(readPkg);

        log.WriteLine("Dictionary JSON:");
        log.WriteLine(json.ToString());

        var dictionary = Assert.IsType<JObject>(json["dictionary"]);
        var entries = Assert.IsType<JObject>(dictionary["entries"]);
        var report = Assert.IsType<JObject>(entries["report"]);
        var crew = Assert.IsType<JObject>(report["crew"]);
        var pilot = Assert.IsType<JObject>(crew["pilot"]);
        var pilotDistance = Assert.IsType<JObject>(crew["pilotDistance"]);
        var pilotValue = Assert.IsType<JObject>(pilot["$value"]);

        Assert.Equal(ContentType, packagePart.ContentType);
        Assert.Equal("Asv.Store.Dictionary", dictionary.Value<string>("format"));
        Assert.Equal("1", dictionary.Value<string>("version"));
        Assert.Equal(Description, dictionary.Value<string>("description"));
        Assert.Equal(typeof(TestPilotDto).FullName, pilot.Value<string>("$type"));
        Assert.Equal(
            typeof(TestPilotDto).Assembly.GetName().Name,
            pilot.Value<string>("$assembly")
        );
        Assert.Equal("P1", pilotValue.Value<string>("Id"));
        Assert.Equal("John", pilotValue.Value<string>("Name"));
        Assert.Equal(typeof(double).FullName, pilotDistance.Value<string>("$type"));
        Assert.Equal(
            typeof(double).Assembly.GetName().Name,
            pilotDistance.Value<string>("$assembly")
        );
        Assert.Equal(ExpectedDistance, pilotDistance.Value<double>("$value"));
    }

    [Fact]
    public void Constructor_LegacyRoot_Throws()
    {
        using var ms = new MemoryStream();
        using var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
        var packagePart = pkg.CreatePart(PartUri, ContentType, CompressionOption.Maximum);
        using (var stream = packagePart.GetStream(FileMode.Create, FileAccess.Write))
        using (var writer = new StreamWriter(stream))
        {
            writer.Write("{\"report\":{}}");
        }

        var ex = Assert.Throws<InvalidOperationException>(() => CreatePart(pkg));

        Assert.Contains("root must be 'dictionary'", ex.Message);
    }

    private JsonDictionaryAsvPackage CreatePart(
        Package pkg,
        CompressionOption compression = CompressionOption.Maximum,
        string description = ""
    )
    {
        var logger = new TestLogger(log, TimeProvider.System, nameof(JsonDictionaryAsvPackageTest));
        var ctx = new AsvPackageContext(new Lock(), pkg, logger);
        return new JsonDictionaryAsvPackage(
            PartUri,
            ctx,
            parent: null,
            contentType: ContentType,
            compression: compression,
            description: description
        );
    }

    private static JObject ReadJson(Package package)
    {
        var packagePart = package.GetPart(PartUri);
        using var stream = packagePart.GetStream(FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(stream);
        return JObject.Parse(reader.ReadToEnd());
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
