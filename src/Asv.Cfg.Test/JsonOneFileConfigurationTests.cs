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
    public class JsonOneFileConfigurationTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly string _workingDir = $"{Environment.CurrentDirectory}\\Test";

        public JsonOneFileConfigurationTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public Task Configuration_Should_Throw_Argument_Exception_If_FolderPath_Is_Null_Or_Empty()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var configuration = new JsonOneFileConfiguration(null, false, null);
            });
            
            Assert.Throws<ArgumentException>(() =>
            {
                var configuration = new JsonOneFileConfiguration(string.Empty, false, null);
            });
            
            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Configuration_Should_Be_Saved_In_Set_Directory()
        {
            CleanTestDirectory();
            
            var cfg = new JsonOneFileConfiguration($"{_workingDir}\\TestClass.json", true, null);
            cfg.Set(new TestClass(){ Name = "Test" });
            var fileName = Directory.GetFiles(_workingDir, "*.json");
            
            Assert.Equal($"{_workingDir}\\TestClass.json", fileName.FirstOrDefault());

            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Configuration_Should_Be_Saved_In_Directory_That_Does_Not_Exist()
        {
            CleanTestDirectory();
            var cfg = new JsonOneFileConfiguration($"{_workingDir}\\NO_SUCH_DIRECTORY\\TestClass.json", true, null);
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
        public Task Configuration_Should_Not_Be_Saved_In_Directory_That_Does_Not_Exist_If_Create_If_Not_Exist_Is_False()
        {
            CleanTestDirectory();

            var fileName = string.Empty;
            
            try
            {
                var cfg = new JsonOneFileConfiguration($"{_workingDir}\\NO_SUCH_DIRECTORY\\TestClass.json", false, null);
                cfg.Set(new TestClass(){ Name = "Test" });
                fileName = Directory.GetFiles($"{_workingDir}\\NO_SUCH_DIRECTORY", "*.json").FirstOrDefault();
            }
            catch (Exception e)
            {
                _testOutputHelper.WriteLine($"Exception occured: {e.Message}");
            }
            
            Assert.False(File.Exists($"{_workingDir}\\NO_SUCH_DIRECTORY\\TestClass.json"));
            Assert.False(Directory.Exists($"{_workingDir}\\NO_SUCH_DIRECTORY"));

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
            var cfg = new JsonOneFileConfiguration($"{_workingDir}\\TestClass.json", true, null);
            cfg.Set("test", new TestClass(){ Name = "Test" });

            var cfgExists = cfg.Exist<TestClass>("test");
            
            Assert.True(cfgExists);

            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Check_If_Not_Saved_Setting_Exists()
        {
            CleanTestDirectory();
            var cfg = new JsonOneFileConfiguration($"{_workingDir}\\TestClass.json", true, null);
            cfg.Set("test", new TestClass(){ Name = "Test" });

            var cfgExists = cfg.Exist<TestClass>("no_test");
            
            Assert.False(cfgExists);

            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Check_If_Multiple_Copies_Of_Same_Setting_Are_Saved()
        {
            CleanTestDirectory();
            var cfg = new JsonOneFileConfiguration($"{_workingDir}\\TestClass.json", true, null);
            cfg.Set(new TestClass(){ Name = "Test" });
            Thread.Sleep(50);
            cfg.Set(new TestClass(){ Name = "Test1" });
            Thread.Sleep(50);
            cfg.Set(new TestClass(){ Name = "Test2" });
            Thread.Sleep(50);
            cfg.Set(new TestClass(){ Name = "Test3" });
            Thread.Sleep(50);

            var testClass = cfg.Get<TestClass>();

            Assert.Equal("Test3", testClass.Name);

            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Check_If_Not_Saved_Setting_Is_Returned_With_Default_Value()
        {
            CleanTestDirectory();
            var cfg = new JsonOneFileConfiguration($"{_workingDir}\\TestClass.json", true, null);
            
            var getFromCfg = cfg.Get<TestNotSaved>();
            
            Assert.Null(getFromCfg.Name);

            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Check_If_Setting_Is_Removed()
        {
            CleanTestDirectory();
            var cfg = new JsonOneFileConfiguration($"{_workingDir}\\TestClass.json", true, null);
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
            var cfg = new JsonOneFileConfiguration($"{_workingDir}\\TestClass.json", true, null);
            
            cfg.Remove("testRemove");
            
            Assert.False(cfg.Exist<TestClass>("testRemove"));

            return Task.CompletedTask;
        }

        [Fact]
        public Task Check_Available_Parts_Value()
        {
            CleanTestDirectory();
            var cfg = new JsonOneFileConfiguration($"{_workingDir}\\TestClass.json", true, null);
            cfg.Set("test1", new TestClass(){ Name = "Test1" });
            Thread.Sleep(50);
            cfg.Set("test2", new TestClass(){ Name = "Test2" });
            Thread.Sleep(50);
            cfg.Set("test3", new TestClass(){ Name = "Test3" });
            Thread.Sleep(50);
            cfg.Set("test4", new TestClass(){ Name = "Test4" });
            Thread.Sleep(50);
            
            var expectedResult = new string[] { "test1", "test2", "test3", "test4" };
            var actualResult = cfg.AvailableParts;
            
            Assert.Equal(expectedResult, actualResult);
            
            return Task.CompletedTask;
        }
        
        //[Fact]
        //No idea how to check if this test passes, results may vary
        public Task Check_For_Multiple_Threads_To_Write_In_Same_Field()
        {
            CleanTestDirectory();
            var cfg = new JsonOneFileConfiguration($"{_workingDir}\\TestClass.json", true, TimeSpan.FromMilliseconds(100));

            var threads = new Thread[4];

            for (var i = 0; i < threads.Length; i++)
            {
                var j = i;
                threads[i] = new Thread(() => cfg.Set(new TestClass() { Name = $"TestMultiThread{j}" }));
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
                //Assert.IsType<IOException>(e);
            }
            Thread.Sleep(500);
            
            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Check_For_Multiple_Threads_To_Write_In_Multiple_Fields()
        {
            CleanTestDirectory();
            var cfg = new JsonOneFileConfiguration($"{_workingDir}\\TestClass.json", true, TimeSpan.FromMilliseconds(100));

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
            
            Thread.Sleep(500);
            
            Assert.True(cfg.Exist<TestMultiThreadClassOne>("one"));
            Assert.True(cfg.Exist<TestMultiThreadClassTwo>("two"));
            Assert.True(cfg.Exist<TestMultiThreadClassThree>("three"));
            Assert.True(cfg.Exist<TestMultiThreadClassFour>("four"));
            
            return Task.CompletedTask;
        }
        
        //TODO: multithreading read test

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