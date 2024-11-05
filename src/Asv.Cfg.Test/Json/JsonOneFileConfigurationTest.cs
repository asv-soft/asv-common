using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reactive.Disposables;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Asv.Cfg.Test
{
    [TestSubject(typeof(JsonOneFileConfiguration))]
    public class JsonOneFileConfigurationTest(ITestOutputHelper log) : ConfigurationBaseTest<JsonOneFileConfiguration>
    {
        private readonly IFileSystem _fileSystem = new MockFileSystem();

        protected override IDisposable CreateForTest(out JsonOneFileConfiguration configuration)
        {
            var filePath = GenerateTempFilePath();
            configuration = new JsonOneFileConfiguration(
                filePath,
                true,
                null,
                logger:new TestLogger(log,TimeProvider.System, "JSON_ONE_FILE"),
                fileSystem: _fileSystem
            );
            
            var cfg = configuration;
            return Disposable.Create(() =>
            {
                cfg.Dispose();
                if (_fileSystem.File.Exists(filePath))
                {
                    _fileSystem.File.Delete(filePath);
                }
            });
        }

        private string GenerateTempFilePath()
        {
            return _fileSystem.Path.Combine(
                _fileSystem.Path.GetTempPath(), 
                _fileSystem.Path.GetFileNameWithoutExtension(
                    _fileSystem.Path.GetRandomFileName()),
                $"{_fileSystem.Path.GetFileNameWithoutExtension(_fileSystem.Path.GetRandomFileName())}.json"
            );
        }

        [Fact]
        public void Configuration_Should_Throw_Argument_Exception_If_FolderPath_Is_Null_Or_Empty()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var configuration = new JsonOneFileConfiguration(
                    null!,
                    false,
                    null,
                    fileSystem: _fileSystem
                );
            });

            Assert.Throws<ArgumentException>(() =>
            {
                var configuration = new JsonOneFileConfiguration(
                    string.Empty,
                    false,
                    null,
                    fileSystem: _fileSystem
                );
            });
            Assert.Throws<ArgumentException>(() =>
            {
                var configuration = new JsonOneFileConfiguration(
                    "   ",
                    false,
                    null,
                    fileSystem: _fileSystem
                );
            });
        }

        [Fact]
        public void Configuration_Should_Be_Saved_In_Set_Directory()
        {
            using var cleanup = CreateForTest(out var cfg);

            cfg.Set(new TestClass() { Name = "Test" });
            var dir = _fileSystem.Path.GetDirectoryName(cfg.FileName);
            var fileName = _fileSystem.Directory.GetFiles(dir ?? throw new InvalidOperationException(), "*.json");

            Assert.Equal(cfg.FileName, fileName.FirstOrDefault());
        }

        [Fact]
        public void Configuration_Should_Be_Saved_In_Directory_That_Does_Not_Exist()
        {
            using var cleanup = CreateForTest(out var cfg);
            cfg.Set(new TestClass() { Name = "Test" });
            var dir = _fileSystem.Path.GetDirectoryName(cfg.FileName);
            var fileName = _fileSystem.Directory.GetFiles(dir ?? throw new InvalidOperationException(), "*.json").FirstOrDefault();

            Assert.Equal(cfg.FileName, fileName);

            if (_fileSystem.File.Exists(fileName))
            {
                _fileSystem.File.Delete(fileName);
            }

            _fileSystem.Directory.Delete(dir);
        }

        [Fact]
        public void Configuration_Should_Not_Be_Saved_In_Directory_That_Does_Not_Exist_If_Create_If_Not_Exist_Is_False()
        {
            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                var cfg = new JsonOneFileConfiguration(
                    GenerateTempFilePath(),
                    false,
                    null,
                    fileSystem: _fileSystem
                );
                
                cfg.Set(new TestClass() { Name = "Test" });
            });
        }
        
        [Fact]
        public void Json_Enum_Should_Deserialized_With_Integers()
        {
            var file = GenerateTempFilePath();
            var dir = _fileSystem.Path.GetDirectoryName(file);
            _fileSystem.Directory.CreateDirectory(dir ?? throw new InvalidOperationException());

            try
            {
                var dict = new Dictionary<string, TestClassWithEnums>();
                dict.Add("test", new TestClassWithEnums { Enum = EnumTest.Test3, Name = "Test" });
                var stringWithEnumAsDigit = JsonConvert.SerializeObject(dict, Formatting.Indented);
                _fileSystem.File.WriteAllText(file, stringWithEnumAsDigit);
                var cfg = new JsonOneFileConfiguration(file, false,
                    null, fileSystem: _fileSystem);
                var result = cfg.Get<TestClassWithEnums>("test");
                Assert.Equal(EnumTest.Test3, result.Enum);
            }
            finally
            {
                _fileSystem.Directory.Delete(dir, true);
            }
        }
        
        [Fact]
        public void Json_Enum_Should_Serialized_With_Names()
        {
            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                var file = GenerateTempFilePath();
                _fileSystem.File.WriteAllText(file, "{\"Enum\":3}");
                var cfg = new JsonOneFileConfiguration(file, false,
                    null, fileSystem: _fileSystem);
                var result = cfg.Get<TestClassWithEnums>();
                Assert.Equal(EnumTest.Test3, result.Enum);
                
            });
        }
    }




}