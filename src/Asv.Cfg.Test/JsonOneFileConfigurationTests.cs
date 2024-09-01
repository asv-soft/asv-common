using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Asv.Cfg.Json;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Asv.Cfg.Test
{
    public class JsonOneFileConfigurationTests : ConfigurationTestBase<JsonOneFileConfiguration>
    {
        private readonly ITestOutputHelper _testOutputHelper;


        public JsonOneFileConfigurationTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public override IDisposable CreateForTest(out JsonOneFileConfiguration configuration)
        {
            var filePath = GenerateTempFilePath();
            configuration = new JsonOneFileConfiguration(filePath, true, null);
            var cfg = configuration;
            return Disposable.Create(() =>
            {
                cfg.Dispose();
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            });
        }

        private string GenerateTempFilePath()
        {
            return Path.Combine(Path.GetTempPath(),Path.GetFileNameWithoutExtension(Path.GetRandomFileName()),$"{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}.json");
        }

        [Fact]
        public void Configuration_Should_Throw_Argument_Exception_If_FolderPath_Is_Null_Or_Empty()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var configuration = new JsonOneFileConfiguration(null, false, null);
            });

            Assert.Throws<ArgumentException>(() =>
            {
                var configuration = new JsonOneFileConfiguration(string.Empty, false, null);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                var configuration = new JsonOneFileConfiguration("   ", false, null);
            });
        }

        [Fact]
        public void Configuration_Should_Be_Saved_In_Set_Directory()
        {
            using var cleanup = CreateForTest(out var cfg);

            cfg.Set(new TestClass() { Name = "Test" });
            var dir = Path.GetDirectoryName(cfg.FileName);
            var fileName = Directory.GetFiles(dir ?? throw new InvalidOperationException(), "*.json");

            Assert.Equal(cfg.FileName, fileName.FirstOrDefault());
        }

        [Fact]
        public void Configuration_Should_Be_Saved_In_Directory_That_Does_Not_Exist()
        {
            using var cleanup = CreateForTest(out var cfg);
            cfg.Set(new TestClass() { Name = "Test" });
            var dir = Path.GetDirectoryName(cfg.FileName);
            var fileName = Directory.GetFiles(dir, "*.json").FirstOrDefault();

            Assert.Equal(cfg.FileName, fileName);

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            Directory.Delete(dir);
        }

        [Fact]
        public void Configuration_Should_Not_Be_Saved_In_Directory_That_Does_Not_Exist_If_Create_If_Not_Exist_Is_False()
        {
            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                var cfg = new JsonOneFileConfiguration(GenerateTempFilePath(), false,
                    null);
                cfg.Set(new TestClass() { Name = "Test" });
            });
        }
        
        [Fact]
        public void Json_Enum_Should_Deserialized_With_Integers()
        {
            var file = GenerateTempFilePath();
            var dir = Path.GetDirectoryName(file);
            Directory.CreateDirectory(dir);
            try
            {
                var dict = new Dictionary<string, TestClassWithEnums>();
                dict.Add("test", new TestClassWithEnums(){Enum = EnumTest.Test3, Name = "Test"});
                var stringWithEnumAsDigit =JsonConvert.SerializeObject(dict, Formatting.Indented);
                File.WriteAllText(file, stringWithEnumAsDigit);
                var cfg = new JsonOneFileConfiguration(file, false,
                    null);
                var readed = cfg.Get<TestClassWithEnums>("test");
                Assert.Equal(EnumTest.Test3, readed.Enum);
            }
            finally
            {
                Directory.Delete(dir,true);
            }
            
        }
        
        [Fact]
        public void Json_Enum_Should_Serialized_With_Names()
        {
            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                var file = GenerateTempFilePath();
                File.WriteAllText(file, "{\"Enum\":3}");
                var cfg = new JsonOneFileConfiguration(file, false,
                    null);
                var readed = cfg.Get<TestClassWithEnums>();
                Assert.Equal(EnumTest.Test3, readed.Enum);
                
            });
        }
    }




}