using System.IO.Packaging;
using System.Text;
using Asv.XUnit;
using JetBrains.Annotations;

namespace Asv.Store.Test;

[TestSubject(typeof(FilesAsvPackagePart))]
public class FilesAsvPackagePartTest(ITestOutputHelper log)
{
    private static readonly Uri RootUri = new("/files/", UriKind.Relative);
    private static readonly Uri ManifestUri = new("/files/manifest.json", UriKind.Relative);
    private static readonly Uri PilotContentUri = new(
        "/files/content/reports/pilot.txt",
        UriKind.Relative
    );
    private const string TextContentType = "text/plain";

    [Fact]
    public void WriteRead_RoundtripWorks()
    {
        using var ms = new MemoryStream();
        var metadata = new Dictionary<string, string> { ["author"] = "crew" };

        using (var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite))
        {
            var part = CreatePart(pkg);
            part.Write("reports/pilot.txt", CreateStream("Pilot report"), TextContentType, metadata);
            part.Dispose();
        }

        ms.Position = 0;
        using var readPkg = Package.Open(ms, FileMode.Open, FileAccess.Read);
        var readPart = CreatePart(readPkg);
        var item = readPart.Get("reports/pilot.txt");

        Assert.NotNull(item);
        Assert.Equal("reports/pilot.txt", item.RelativePath);
        Assert.Equal("pilot.txt", item.Name);
        Assert.Equal(TextContentType, item.ContentType);
        Assert.Equal("crew", item.Metadata["author"]);
        Assert.Equal(Encoding.UTF8.GetByteCount("Pilot report"), item.Length);
        Assert.Equal("Pilot report", ReadText(item));
    }

    [Fact]
    public void Write_OverwritesExistingFile()
    {
        using var ms = new MemoryStream();
        using var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
        var part = CreatePart(pkg);

        part.Write("reports/pilot.txt", CreateStream("First"), TextContentType);
        part.Write(
            "reports/pilot.txt",
            CreateStream("Second"),
            "application/json",
            new Dictionary<string, string> { ["revision"] = "2" }
        );

        var item = part.Get("reports/pilot.txt");
        Assert.NotNull(item);
        Assert.Equal("application/json", item.ContentType);
        Assert.Equal("2", item.Metadata["revision"]);
        Assert.Equal("Second", ReadText(item));
    }

    [Fact]
    public void Delete_RemovesContentAndMetadata()
    {
        using var ms = new MemoryStream();
        using var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
        var part = CreatePart(pkg);
        part.Write("reports/pilot.txt", CreateStream("Pilot report"), TextContentType);

        Assert.True(part.Exists("reports/pilot.txt"));
        Assert.True(part.Delete("reports/pilot.txt"));

        Assert.False(part.Exists("reports/pilot.txt"));
        Assert.Null(part.Get("reports/pilot.txt"));
        Assert.False(pkg.PartExists(PilotContentUri));
        Assert.False(pkg.PartExists(ManifestUri));
        Assert.False(part.Delete("reports/pilot.txt"));
    }

    [Fact]
    public void Enumerate_FiltersByDirectory()
    {
        using var ms = new MemoryStream();
        using var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
        var part = CreatePart(pkg);

        part.Write("reports/pilot.txt", CreateStream("Pilot"), TextContentType);
        part.Write("reports/archive/old.txt", CreateStream("Old"), TextContentType);
        part.Write("other.txt", CreateStream("Other"), TextContentType);

        var shallow = part.Enumerate("reports", recursive: false).Select(x => x.RelativePath).ToArray();
        var recursive = part.Enumerate("reports").Select(x => x.RelativePath).ToArray();

        Assert.Equal(["reports/pilot.txt"], shallow);
        Assert.Equal(["reports/archive/old.txt", "reports/pilot.txt"], recursive);
    }

    [Fact]
    public void Write_CreatesManifestAndContentParts()
    {
        using var ms = new MemoryStream();
        using var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
        var part = CreatePart(pkg);

        part.Write("reports/pilot.txt", CreateStream("Pilot report"), TextContentType);

        Assert.True(pkg.PartExists(ManifestUri));
        Assert.True(pkg.PartExists(PilotContentUri));
        Assert.Equal(TextContentType, pkg.GetPart(PilotContentUri).ContentType);
        log.WriteLine($"Manifest URI: {ManifestUri}");
        log.WriteLine($"Content URI: {PilotContentUri}");
    }

    [Theory]
    [InlineData("/absolute.txt")]
    [InlineData("../outside.txt")]
    [InlineData("reports/../outside.txt")]
    [InlineData("reports/")]
    public void Write_InvalidRelativePath_Throws(string relativePath)
    {
        using var ms = new MemoryStream();
        using var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
        var part = CreatePart(pkg);

        Assert.Throws<ArgumentException>(() =>
            part.Write(relativePath, CreateStream("Invalid"), TextContentType)
        );
    }

    private FilesAsvPackagePart CreatePart(Package pkg)
    {
        var logger = new TestLogger(log, TimeProvider.System, nameof(FilesAsvPackagePartTest));
        var ctx = new AsvPackageContext(new Lock(), pkg, logger);
        return new FilesAsvPackagePart(RootUri, ctx);
    }

    private static MemoryStream CreateStream(string value)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(value));
    }

    private static string ReadText(IStoredFile item)
    {
        using var stream = item.OpenRead();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
