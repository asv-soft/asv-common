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
        _logger = LogFactory.CreateLogger("ZIP_JSON_VERSIONED");
    }

    // Вспомогательный метод для создания тестового файла
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
        // Arrange
        var version = new SemVersion(1, 0, 0);
        const string fileType = "TestType";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            var config = new ZipJsonVersionedFile(null!, version, fileType, true, true, _logger);
        });
    }

    [Fact]
    public void Constructor_CreateIfNotExistTrue_CreatesNewFile()
    {
        // Arrange
        var filePath = GenerateTempFilePath();
        var dir = _fileSystem.Path.GetDirectoryName(filePath);
        _fileSystem.Directory.CreateDirectory(dir);
        var file = _fileSystem.File.Open(filePath, FileMode.OpenOrCreate);
        var version = new SemVersion(1, 0, 0);
        const string fileType = "TestType";

        // Act
        var config = new ZipJsonVersionedFile(file, version, fileType, true, true, _logger);

        // Assert
        Assert.Equal(version, config.FileVersion);
        config.Dispose();
    }

    [Fact]
    public void Constructor_CreateIfNotExistFalse_ThrowsConfigurationException()
    {
        // Arrange
        var filePath = GenerateTempFilePath();
        var dir = _fileSystem.Path.GetDirectoryName(filePath);
        _fileSystem.Directory.CreateDirectory(dir);
        var file = _fileSystem.File.Open(filePath, FileMode.OpenOrCreate);
        var version = new SemVersion(1, 0, 0);
        const string fileType = "TestType";

        // Act & Assert
        Assert.Throws<ConfigurationException>(() => new ZipJsonVersionedFile(file, version, fileType, false, true, _logger));
    }

    [Fact]
    public void Constructor_VersionGreaterThanSupported_ThrowsConfigurationException()
    {
        // Arrange
        var filePath = GenerateTempFilePath();
        var dir = _fileSystem.Path.GetDirectoryName(filePath);
        _fileSystem.Directory.CreateDirectory(dir);
        var file = _fileSystem.File.Open(filePath, FileMode.OpenOrCreate);

        var newVersion = new SemVersion(2, 0, 0);
        const string fileType = "TestType";
        var config = new ZipJsonVersionedFile(file, newVersion, fileType, true, true, _logger);
        config.Dispose();

        file = _fileSystem.File.Open(filePath, FileMode.Open);
        var oldVersion = new SemVersion(1, 0, 0);

        // Act & Assert
        Assert.Throws<ConfigurationException>(() => new ZipJsonVersionedFile(file, oldVersion, fileType, false, true, _logger));
    }

    [Fact]
    public void Constructor_DifferentFileType_ThrowsConfigurationException()
    {
        // Arrange
        var filePath = GenerateTempFilePath();
        var dir = _fileSystem.Path.GetDirectoryName(filePath);
        _fileSystem.Directory.CreateDirectory(dir);
        var file = _fileSystem.File.Open(filePath, FileMode.OpenOrCreate);

        var version = new SemVersion(1, 0, 0);
        const string fileType1 = "TestType1";
        var config = new ZipJsonVersionedFile(file, version, fileType1, true, true, _logger);
        config.Dispose();

        file = _fileSystem.File.Open(filePath, FileMode.Open);
        const string fileType2 = "TestType2";

        // Act & Assert
        Assert.Throws<ConfigurationException>(() => new ZipJsonVersionedFile(file, version, fileType2, false, true, _logger));
    }

    [Fact]
    public void Constructor_ValidVersionAndType_OpensExistingFile()
    {
        // Arrange
        var filePath = GenerateTempFilePath();
        var dir = _fileSystem.Path.GetDirectoryName(filePath);
        _fileSystem.Directory.CreateDirectory(dir);
        var file = _fileSystem.File.Open(filePath, FileMode.OpenOrCreate);

        var version = new SemVersion(1, 0, 0);
        const string fileType = "TestType";

        var config = new ZipJsonVersionedFile(file, version, fileType, true, true, _logger);
        config.Dispose();

        // Act
        file = _fileSystem.File.Open(filePath, FileMode.Open);
        var openedConfig = new ZipJsonVersionedFile(file, version, fileType, false, true, _logger);

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