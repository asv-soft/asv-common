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
public class ZipJsonVersionedFileTest : ConfigurationBaseTest<ZipJsonVersionedFile>
{
    private readonly ITestOutputHelper _log;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;

    public ZipJsonVersionedFileTest(ITestOutputHelper log) : base(log)
    {
        _log = log;
        _fileSystem = new MockFileSystem();
    }

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
                "test", true, false, _logger));
    }

    [Fact]
    public void Constructor_CreateIfNotExistTrue_CreatesNewFile()
    {
        // Act
        using var disp = CreateForTest(out var config);

        // Assert
        Assert.Equal(new SemVersion(1, 0), config.FileVersion);
    }

    [Fact]
    public void Constructor_CreateIfNotExistFalse_ThrowsConfigurationException()
    {
        Assert.Throws<ConfigurationException>(() =>
            new ZipJsonVersionedFile(new MemoryStream(), new SemVersion(1, 0),
                "test", false, false, _logger));
    }

    [Fact]
    public void Constructor_VersionGreaterThanSupported_ThrowsConfigurationException()
    {
        // Arrange
        var stream = new MemoryStream();
        var config = new ZipJsonVersionedFile(stream, new SemVersion(2, 0), "test", true, true, _logger);
        config.Dispose();

        // Act & Assert
        Assert.Throws<ConfigurationException>(() =>
            new ZipJsonVersionedFile(stream, new SemVersion(1, 0), "test", false, true, _logger));
    }

    [Fact]
    public void Constructor_DifferentFileType_ThrowsConfigurationException()
    {
        // Arrange
        var stream = new MemoryStream();
        var config = new ZipJsonVersionedFile(stream, new SemVersion(1, 0), "test1", true, true, _logger);
        config.Dispose();

        // Act & Assert
        Assert.Throws<ConfigurationException>(() =>
            new ZipJsonVersionedFile(stream, new SemVersion(1, 0), "test2", false, true, _logger));
    }

    [Fact]
    public void Constructor_ValidVersionAndType_OpensExistingFile()
    {
        // Arrange
        var stream = new MemoryStream();
        var version = new SemVersion(1, 0);
        var fileType = "test";

        var config = new ZipJsonVersionedFile(stream, version, fileType, true, true, _logger);
        config.Dispose();

        // Act
        var openedConfig = new ZipJsonVersionedFile(stream, version, fileType, false, true, _logger);

        // Assert
        Assert.Equal(version, openedConfig.FileVersion);
        openedConfig.Dispose();
    }

    protected override IDisposable CreateForTest(out ZipJsonVersionedFile configuration)
    {
        configuration = new ZipJsonVersionedFile(new MemoryStream(), new SemVersion(1, 0), "test", true, false,
            LogFactory.CreateLogger("ZIP_VERSION_JSON_CONFIG"));
        return Disposable.Empty;
    }
}