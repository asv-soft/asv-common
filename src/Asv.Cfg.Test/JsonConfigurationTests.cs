using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asv.Cfg.Json;
using Xunit;
using Xunit.Abstractions;

namespace Asv.Cfg.Test
{
    #region Test Classes
    public class TestNotSaved
    {
        public string Name { get; set; }
    }
    
    public class TestClass
    {
        public string Name { get; set; }
    }
    
    public class TestMultiThreadClassOne
    {
        public string Name { get; set; }
    }
    
    public class TestMultiThreadClassTwo
    {
        public string Name { get; set; }
    }
    
    public class TestMultiThreadClassThree
    {
        public string Name { get; set; }
    }
    
    public class TestMultiThreadClassFour
    {
        public string Name { get; set; }
    }
    #endregion
    
    public class JsonConfigurationTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly string _workingDir = $"{Environment.CurrentDirectory}\\Test";

        public JsonConfigurationTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public Task Configuration_Should_Throw_Argument_Null_Exception_If_FolderPath_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var configuration = new JsonConfiguration(null);
            });
            
            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Configuration_Should_Throw_Argument_Exception_If_FolderPath_Is_Empty()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var configuration = new JsonConfiguration(string.Empty);
            });
            
            return Task.CompletedTask;
        }

        [Fact]
        public Task Configuration_Should_Be_Saved_In_Set_Directory()
        {
            CleanTestDirectory();
            var cfg = new JsonConfiguration(_workingDir);
            cfg.Set(new TestClass(){ Name = "Test" });
            var fileName = Directory.GetFiles(_workingDir, "*.json");
            
            Assert.Equal($"{_workingDir}\\TestClass.json", fileName.FirstOrDefault());

            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Configuration_Should_Be_Saved_In_Directory_That_Does_Not_Exist()
        {
            CleanTestDirectory();
            var cfg = new JsonConfiguration($"{_workingDir}\\NO_SUCH_DIRECTORY");
            cfg.Set(new TestClass(){ Name = "Test" });
            var fileName = Directory.GetFiles($"{_workingDir}\\NO_SUCH_DIRECTORY", "*.json").FirstOrDefault();
            
            Assert.Equal($"{_workingDir}\\NO_SUCH_DIRECTORY\\TestClass.json", fileName);

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            
            Directory.Delete($"{_workingDir}\\NO_SUCH_DIRECTORY");

            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Check_If_Saved_Setting_Exists()
        {
            CleanTestDirectory();
            var cfg = new JsonConfiguration(_workingDir);
            cfg.Set("test", new TestClass(){ Name = "Test" });

            var cfgExists = cfg.Exist<TestClass>("test");
            var fileExists = File.Exists(Path.Combine(_workingDir, "test.json"));
            
            Assert.True(cfgExists == fileExists);

            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Check_If_Not_Saved_Setting_Exists()
        {
            CleanTestDirectory();
            var cfg = new JsonConfiguration(_workingDir);
            cfg.Set("test", new TestClass(){ Name = "Test" });

            var cfgExists = cfg.Exist<TestClass>("no_test");
            var fileExists = File.Exists(Path.Combine(_workingDir, "no_test.json"));
            
            Assert.True(cfgExists == fileExists);

            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Check_If_Multiple_Copies_Of_Same_Setting_Are_Saved()
        {
            CleanTestDirectory();
            var cfg = new JsonConfiguration(_workingDir);
            cfg.Set(new TestClass(){ Name = "Test" });
            Thread.Sleep(50);
            cfg.Set(new TestClass(){ Name = "Test1" });
            Thread.Sleep(50);
            cfg.Set(new TestClass(){ Name = "Test2" });
            Thread.Sleep(50);
            cfg.Set(new TestClass(){ Name = "Test3" });
            Thread.Sleep(50);

            var dirInfo = new DirectoryInfo(_workingDir);
            var files = dirInfo.GetFiles();

            var fileQuery =
                from file in files
                where file.Name == "TestClass.json"
                select file;

            var fileInfos = fileQuery as FileInfo[] ?? fileQuery.ToArray();

            Assert.Single(fileInfos);

            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Check_If_Saved_Setting_Is_Returned_Correctly()
        {
            CleanTestDirectory();
            var cfg = new JsonConfiguration(_workingDir);
            var testClass = new TestClass() { Name = "TestGet" };
            cfg.Set(testClass);

            var getFromCfg = cfg.Get<TestClass>();
            
            Assert.Equal(testClass.Name, getFromCfg.Name);

            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Check_If_Not_Saved_Setting_Is_Returned_With_Default_Value()
        {
            CleanTestDirectory();
            var cfg = new JsonConfiguration(_workingDir);
            
            var getFromCfg = cfg.Get<TestNotSaved>();
            
            Assert.Null(getFromCfg.Name);

            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Check_If_Setting_Is_Removed()
        {
            CleanTestDirectory();
            var cfg = new JsonConfiguration(_workingDir);
            var testClass = new TestClass() { Name = "TestRemove" };
            cfg.Set("testRemove", testClass);
            
            cfg.Remove("testRemove");
            
            Assert.False(cfg.Exist<TestClass>("testRemove"));

            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Check_If_Removing_Not_Saved_Setting_Does_Nothing()
        {
            CleanTestDirectory();
            var cfg = new JsonConfiguration(_workingDir);
            
            cfg.Remove("testRemove");
            
            Assert.False(cfg.Exist<TestClass>("testRemove"));

            return Task.CompletedTask;
        }

        [Fact]
        public async Task Check_For_Multiple_Threads_To_Write_In_Same_Field()
        {
            CleanTestDirectory();
            var cfg = new JsonConfiguration(_workingDir);

            var threads = new Thread[4];

            for (var i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(() => cfg.Set(new TestClass() { Name = $"TestMultiThread{i}" }));
            }

            foreach (var thread in threads)
            {
                thread.Start();
            }

            try
            {
                foreach (var thread in threads)
                {
                    thread.Join();
                }
            }
            catch (Exception e)
            {
                Assert.IsType<IOException>(e);
            }
        }
        
        [Fact]
        public async Task Check_For_Multiple_Threads_To_Write_In_Multiple_Fields()
        {
            CleanTestDirectory();
            var cfg = new JsonConfiguration(_workingDir);

            var threads = new Thread[4];

            threads[0] = new Thread(() => cfg.Set("one", new TestMultiThreadClassOne() { Name = "First" }));
            threads[1] = new Thread(() => cfg.Set("two", new TestMultiThreadClassTwo() { Name = "Second" }));
            threads[2] = new Thread(() => cfg.Set("three", new TestMultiThreadClassThree() { Name = "Third" }));
            threads[3] = new Thread(() => cfg.Set("four", new TestMultiThreadClassFour() { Name = "Fourth" }));

            foreach (var thread in threads)
            {
                thread.Start();
            }

            try
            {
                foreach (var thread in threads)
                {
                    thread.Join();
                }
            }
            catch (Exception e)
            {
                _testOutputHelper.WriteLine($"Test failed with exception {e.Message}");
                throw;
            }
            
            Assert.True(cfg.Exist<TestMultiThreadClassOne>("one"));
            Assert.True(cfg.Exist<TestMultiThreadClassTwo>("two"));
            Assert.True(cfg.Exist<TestMultiThreadClassThree>("three"));
            Assert.True(cfg.Exist<TestMultiThreadClassFour>("four"));
        }
        
        private void CleanTestDirectory()
        {
            if (!Directory.Exists(_workingDir)) return;
            
            var files = Directory.GetFiles(_workingDir, "*.json");
            
            if (files.Length <= 0) return;
            
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }
    }
}