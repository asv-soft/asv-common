using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using JetBrains.Annotations;
using R3;
using Xunit;
using Xunit.Abstractions;

namespace Asv.Cfg.Test
{
    [TestSubject(typeof(ZipJsonConfiguration))]
    public class ZipJsonConfigurationTest(ITestOutputHelper log)
        : ConfigurationBaseTest<ZipJsonConfiguration>(log)
    {
        private readonly IFileSystem _fileSystem = new MockFileSystem();

        [Fact]
        public void Configuration_Should_Throw_Argument_Exception_If_Null()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var configuration = new ZipJsonConfiguration(null!);
            });
        }

        protected override IDisposable CreateForTest(out ZipJsonConfiguration configuration)
        {
            var filePath = GenerateTempFilePath();
            var dir = _fileSystem.Path.GetDirectoryName(filePath);
            if (_fileSystem.Directory.Exists(dir) == false)
            {
                _fileSystem.Directory.CreateDirectory(dir ?? throw new InvalidOperationException());
            }
            var file = _fileSystem.File.Open(filePath, FileMode.OpenOrCreate);
            configuration = new ZipJsonConfiguration(
                file,
                true,
                logger: LogFactory.CreateLogger("ZIP_JSON")
            );
            var cfg = configuration;
            return Disposable.Create(() =>
            {
                cfg.Dispose();
                file.Dispose();
                _fileSystem.Directory.Delete(dir, true);
            });
        }

        private string GenerateTempFilePath()
        {
            return _fileSystem.Path.Join(
                _fileSystem.Path.GetTempPath(),
                _fileSystem.Path.GetRandomFileName(),
                $"{_fileSystem.Path.GetRandomFileName()}.zip"
            );
        }
    }
}
