using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asv.Cfg.Json;
using DeepEqual.Syntax;
using Xunit;
using Xunit.Abstractions;

namespace Asv.Cfg.Test
{
    
    
    public class ZipJsonConfigurationTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly string _workingDir = $"{Path.GetTempPath()}\\{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}\\Test";

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

        [Fact]
        public void Configuration_Should_Be_Saved_And_Loaded()
        {
            var fileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var file = File.Open(fileName, FileMode.OpenOrCreate);
            var cfg = new ZipJsonConfiguration(file);
            var origin = new TestClass() { Name = "Test" };
            cfg.Set(origin);
            var loaded = cfg.Get<TestClass>();
            loaded.ShouldDeepEqual(origin);
            cfg.Dispose();
            File.Delete(fileName);
        }
        
        
        [Fact]
        public void Check_For_Multiple_Threads_To_Write_In_Multiple_Fields()
        {
            var fileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var file = File.Open(fileName, FileMode.OpenOrCreate);
            var cfg = new ZipJsonConfiguration(file);
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
            cfg.Dispose();
            File.Delete(fileName);
            
        }
        
        
    }
}