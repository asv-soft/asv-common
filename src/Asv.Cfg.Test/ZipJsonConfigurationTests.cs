using System;
using System.IO;
using System.Reactive.Disposables;
using Asv.Cfg.Json;
using Xunit;
using Xunit.Abstractions;

namespace Asv.Cfg.Test
{
    
    
    public class ZipJsonConfigurationTests: ConfigurationTestBase<ZipJsonConfiguration>
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ZipJsonConfigurationTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Configuration_Should_Throw_Argument_Exception_If_Null()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var configuration = new ZipJsonConfiguration(null);
            });
            
            
        }
        
        
        public override IDisposable CreateForTest(out ZipJsonConfiguration configuration)
        {
            var filePath = GenerateTempFilePath();
            var dir = Path.GetDirectoryName(filePath);
            if (Directory.Exists(dir) == false)
            {
                Directory.CreateDirectory(dir);
            }
            var file = File.Open(filePath, FileMode.OpenOrCreate);
            configuration = new ZipJsonConfiguration(file, true, null);
            var cfg = configuration;
            return Disposable.Create(() =>
            {
                cfg.Dispose();
                file.Dispose();
                Directory.Delete(dir,true);
            });
        }

        private string GenerateTempFilePath()
        {
            return Path.Join(Path.GetTempPath(), Path.GetRandomFileName(), $"{Path.GetRandomFileName()}.zip");
        }
    }
}