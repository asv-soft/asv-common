using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Asv.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using R3;
using Xunit;
using Xunit.Abstractions;

namespace Asv.Cfg.Test;

[TestSubject(typeof(ZipJsonVersionedFile))]
public class ZipJsonVersionedFileTest(ITestOutputHelper log) : ConfigurationBaseTest<ZipJsonVersionedFile>(log)
{
    private readonly IFileSystem _fileSystem = new MockFileSystem();

    private string GenerateTempFilePath()
    {
        return _fileSystem.Path.Join(
            _fileSystem.Path.GetTempPath(),
            _fileSystem.Path.GetRandomFileName(),
            $"{_fileSystem.Path.GetRandomFileName()}.zip"
        );
    }

    [Fact]
    public void Constructor_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ZipJsonVersionedFile(null!, new SemVersion(1, 0),
                "test", true, false));
    }

    [Fact]
    public void Constructor_CreateIfNotExistFalse_ThrowsConfigurationException()
    {
        Assert.Throws<ConfigurationException>(() =>
            new ZipJsonVersionedFile(new MemoryStream(), new SemVersion(1, 0),
                "test", false, false));
    }

    [Fact]
    public void Constructor_VersionGreaterThanSupported_ThrowsConfigurationException()
    {
        // Arrange
        var validStream = new MemoryStream();
        using var config = new ZipJsonVersionedFile(validStream, new SemVersion(2, 0), "test", true, true);

        // Act & Assert
        using var invalidStream = new MemoryStream(validStream.ToArray());
        Assert.Throws<ConfigurationException>(() =>
            new ZipJsonVersionedFile(invalidStream, new SemVersion(1, 0), "test", false, true));
    }

    [Fact]
    public void Constructor_ValidVersionAndType_OpensExistingFile()
    {
        // Arrange
        var memoryStream = new MemoryStream();
        var version = new SemVersion(1, 0);
        var fileType = "test";

        // Act
        using (var config = new ZipJsonVersionedFile(memoryStream, version, fileType, true, true)) 
            memoryStream.Position = 0;
        using var reopenedConfig = new ZipJsonVersionedFile(memoryStream, version, fileType, false, true);

        // Assert
        Assert.Equal(version, reopenedConfig.FileVersion);
    }

    protected override IDisposable CreateForTest(out ZipJsonVersionedFile configuration)
    {
        configuration = new ZipJsonVersionedFile(new MemoryStream(), new SemVersion(1, 0), "test", true, false,
            LogFactory.CreateLogger("ZIP_VERSION_JSON_CONFIG"));
        return Disposable.Empty;
    }
}