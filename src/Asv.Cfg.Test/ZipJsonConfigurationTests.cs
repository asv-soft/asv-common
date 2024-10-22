using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Reactive.Disposables;
using Asv.Cfg.Json;
using Xunit;
using Xunit.Abstractions;

namespace Asv.Cfg.Test
{
    public class ZipJsonConfigurationTests: ConfigurationTestBase<ZipJsonConfiguration>
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly IFileSystem _fileSystem;

        public ZipJsonConfigurationTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _fileSystem = new MockFileSystem();
        }

        [Fact]
        public void Configuration_Should_Throw_Argument_Exception_If_Null()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var configuration = new ZipJsonConfiguration(null!, fileSystem: _fileSystem);
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
            configuration = new ZipJsonConfiguration(file, true, null, fileSystem: _fileSystem);
            var cfg = configuration;
            return Disposable.Create(() =>
            {
                cfg.Dispose();
                file.Dispose();
                _fileSystem.Directory.Delete(dir,true);
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