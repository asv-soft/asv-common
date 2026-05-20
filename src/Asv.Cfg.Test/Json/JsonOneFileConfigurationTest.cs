using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using R3;
using Xunit;

namespace Asv.Cfg.Test
{
    [TestSubject(typeof(JsonOneFileConfiguration))]
    public class JsonOneFileConfigurationTest(ITestOutputHelper log)
        : ConfigurationBaseTest<JsonOneFileConfiguration>(log)
    {
        private readonly IFileSystem _fileSystem = new MockFileSystem();

        protected override IDisposable CreateForTest(out JsonOneFileConfiguration configuration)
        {
            var filePath = GenerateTempFilePath();
            configuration = new JsonOneFileConfiguration(
                filePath,
                true,
                null,
                logger: LogFactory.CreateLogger("JSON_ONE_FILE"),
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
                _fileSystem.Path.GetFileNameWithoutExtension(_fileSystem.Path.GetRandomFileName()),
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
            var fileName = _fileSystem.Directory.GetFiles(
                dir ?? throw new InvalidOperationException(),
                "*.json"
            );

            Assert.Equal(cfg.FileName, fileName.FirstOrDefault());
        }

        [Fact]
        public void Configuration_Should_Be_Saved_In_Directory_That_Does_Not_Exist()
        {
            using var cleanup = CreateForTest(out var cfg);
            cfg.Set(new TestClass() { Name = "Test" });
            var dir = _fileSystem.Path.GetDirectoryName(cfg.FileName);
            var fileName = _fileSystem
                .Directory.GetFiles(dir ?? throw new InvalidOperationException(), "*.json")
                .FirstOrDefault();

            Assert.Equal(cfg.FileName, fileName);

            if (_fileSystem.File.Exists(fileName))
            {
                _fileSystem.File.Delete(fileName);
            }

            _fileSystem.Directory.Delete(dir, true);
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
                var cfg = new JsonOneFileConfiguration(file, false, null, fileSystem: _fileSystem);
                var result = cfg.Get<TestClassWithEnums>("test");
                Assert.Equal(EnumTest.Test3, result.Enum);
            }
            finally
            {
                _fileSystem.Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void Json_Enum_Should_Deserialized_With_Names_Success()
        {
            var file = GenerateTempFilePath();
            var dir = _fileSystem.Path.GetDirectoryName(file);
            _fileSystem.Directory.CreateDirectory(dir ?? throw new InvalidOperationException());

            try
            {
                var dict = new Dictionary<string, TestClassWithEnums>();
                dict.Add("test", new TestClassWithEnums { Enum = EnumTest.Test3, Name = "Test" });
                var stringWithEnumAsDigit = JsonConvert.SerializeObject(
                    dict,
                    Formatting.Indented,
                    new StringEnumConverter()
                );
                _fileSystem.File.WriteAllText(file, stringWithEnumAsDigit);
                var cfg = new JsonOneFileConfiguration(file, false, null, fileSystem: _fileSystem);
                var result = cfg.Get<TestClassWithEnums>("test");
                Assert.Equal(EnumTest.Test3, result.Enum);
            }
            finally
            {
                _fileSystem.Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void Configuration_Should_Save_To_Original_File_When_Current_Directory_Changes()
        {
            var firstDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "first");
            var secondDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "second");
            _fileSystem.Directory.CreateDirectory(firstDir);
            _fileSystem.Directory.CreateDirectory(secondDir);
            _fileSystem.Directory.SetCurrentDirectory(firstDir);

            using var cfg = new JsonOneFileConfiguration(
                "settings.json",
                true,
                null,
                fileSystem: _fileSystem
            );
            var expectedFile = _fileSystem.Path.GetFullPath(
                _fileSystem.Path.Combine(firstDir, "settings.json")
            );

            _fileSystem.Directory.SetCurrentDirectory(secondDir);
            cfg.Set("test", new TestClass { Name = "Test" });

            Assert.Equal(expectedFile, cfg.FileName);
            Assert.True(_fileSystem.File.Exists(expectedFile));
            Assert.False(
                _fileSystem.File.Exists(_fileSystem.Path.Combine(secondDir, "settings.json"))
            );
        }

        [Fact]
        public void Configuration_Should_Not_Restore_Backup_When_Create_If_Not_Exist_Is_False()
        {
            var file = GenerateTempFilePath();
            var dir = _fileSystem.Path.GetDirectoryName(file);
            _fileSystem.Directory.CreateDirectory(dir ?? throw new InvalidOperationException());
            var fullPath = _fileSystem.Path.GetFullPath(file);
            _fileSystem.File.WriteAllText(fullPath + ".backup", "{}");

            Assert.Throws<ConfigurationException>(() =>
                new JsonOneFileConfiguration(file, false, null, fileSystem: _fileSystem)
            );

            Assert.False(_fileSystem.File.Exists(fullPath));
        }

        [Fact]
        public void Configuration_Should_Not_Restore_Invalid_Backup()
        {
            var file = GenerateTempFilePath();
            var dir = _fileSystem.Path.GetDirectoryName(file);
            _fileSystem.Directory.CreateDirectory(dir ?? throw new InvalidOperationException());
            var fullPath = _fileSystem.Path.GetFullPath(file);
            _fileSystem.File.WriteAllText(fullPath + ".backup", "{");

            using var cfg = new JsonOneFileConfiguration(file, true, null, fileSystem: _fileSystem);

            Assert.Equal("{}", _fileSystem.File.ReadAllText(cfg.FileName));
            Assert.False(_fileSystem.File.Exists(fullPath + ".backup"));
        }

        [Fact]
        public void Configuration_Should_Restore_Valid_Backup_When_Create_If_Not_Exist_Is_True()
        {
            var file = GenerateTempFilePath();
            var dir = _fileSystem.Path.GetDirectoryName(file);
            _fileSystem.Directory.CreateDirectory(dir ?? throw new InvalidOperationException());
            var fullPath = _fileSystem.Path.GetFullPath(file);
            _fileSystem.File.WriteAllText(fullPath + ".backup", "{\"test\":{\"Name\":\"Backup\"}}");

            using var cfg = new JsonOneFileConfiguration(file, true, null, fileSystem: _fileSystem);
            var value = cfg.Get<TestClass>("test");

            Assert.Equal("Backup", value.Name);
            Assert.True(_fileSystem.File.Exists(fullPath));
        }

        [Fact]
        public void Configuration_Should_Use_Provided_Logger_For_Save_Errors()
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var fullPath = Path.Combine(dir, "settings.json");
            var logger = new ListLogger();
            File.WriteAllText(fullPath, "{}");

            try
            {
                using var cfg = new JsonOneFileConfiguration(
                    fullPath,
                    false,
                    null,
                    logger: logger,
                    fileSystem: new FileSystem()
                );

                File.Delete(fullPath);
                Directory.CreateDirectory(fullPath);

                cfg.Set("test", new TestClass { Name = "Test" });

                Assert.True(
                    SpinWait.SpinUntil(
                        () => logger.Levels.Contains(LogLevel.Error),
                        TimeSpan.FromSeconds(1)
                    )
                );
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void Configuration_Should_Not_Call_End_Save_Changes_When_Write_Fails()
        {
            using var cfg = new FailingJsonConfiguration();

            cfg.Set("test", new TestClass { Name = "Test" });

            Assert.True(
                SpinWait.SpinUntil(() => cfg.BeginSaveChangesCount != 0, TimeSpan.FromSeconds(1))
            );
            Assert.Equal(0, cfg.EndSaveChangesCount);
        }

        [Fact]
        public void Json_Enum_Should_Serialized_With_Names()
        {
            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                var file = GenerateTempFilePath();
                _fileSystem.File.WriteAllText(file, "{\"Enum\":3}");
                var cfg = new JsonOneFileConfiguration(file, false, null, fileSystem: _fileSystem);
                var result = cfg.Get<TestClassWithEnums>();
                Assert.Equal(EnumTest.Test3, result.Enum);
            });
        }

        private sealed class ListLogger : ILogger
        {
            public List<LogLevel> Levels { get; } = [];

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter
            )
            {
                Levels.Add(logLevel);
            }
        }

        private sealed class FailingJsonConfiguration : JsonConfigurationBase
        {
            public FailingJsonConfiguration()
                : base(() => new MemoryStream([(byte)'{', (byte)'}'])) { }

            public int EndSaveChangesCount { get; private set; }

            public int BeginSaveChangesCount { get; private set; }

            protected override void EndSaveChanges()
            {
                EndSaveChangesCount++;
            }

            protected override Stream BeginSaveChanges()
            {
                BeginSaveChangesCount++;
                return new ThrowOnWriteStream();
            }

            protected override IEnumerable<string> InternalSafeGetReservedParts()
            {
                return [];
            }
        }

        private sealed class ThrowOnWriteStream : Stream
        {
            public override bool CanRead => false;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() { }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new IOException("Write failed");
            }
        }
    }
}
