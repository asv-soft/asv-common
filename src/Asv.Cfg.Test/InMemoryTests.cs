using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Asv.Cfg.ImMemory;
using Xunit;
using Xunit.Abstractions;

namespace Asv.Cfg.Test
{
    public class InMemoryTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public InMemoryTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }
        
        [Fact]
        public Task Check_If_Saved_Setting_Exists()
        {
            var cfg = new InMemoryConfiguration();
            cfg.Set("test", new TestClass(){ Name = "Test" });

            Assert.True(cfg.Exist<TestClass>("test"));

            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Check_If_Not_Saved_Setting_Exists()
        {
            var cfg = new InMemoryConfiguration();
            cfg.Set("test", new TestClass(){ Name = "Test" });

            Assert.False(cfg.Exist<TestClass>("no_test"));

            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Check_If_Multiple_Copies_Of_Same_Setting_Are_Saved()
        {
            var cfg = new InMemoryConfiguration();
            cfg.Set(new TestClass(){ Name = "Test" });
            Thread.Sleep(50);
            cfg.Set(new TestClass(){ Name = "Test1" });
            Thread.Sleep(50);
            cfg.Set(new TestClass(){ Name = "Test2" });
            Thread.Sleep(50);
            cfg.Set(new TestClass(){ Name = "Test3" });
            Thread.Sleep(50);

            var actualResult = cfg.Get<TestClass>();
            Assert.Equal("Test3", actualResult.Name);

            return Task.CompletedTask;
        }

        [Fact]
        public Task Check_If_Saved_Setting_Is_Returned_Correctly()
        {
            var cfg = new InMemoryConfiguration();
            var testClass = new TestClass() { Name = "TestGet" };
            cfg.Set(testClass);

            var getFromCfg = cfg.Get<TestClass>();
            
            Assert.Equal(testClass.Name, getFromCfg.Name);

            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Check_If_Not_Saved_Setting_Is_Returned_With_Default_Value()
        {
            var cfg = new InMemoryConfiguration();
            
            var getFromCfg = cfg.Get<TestNotSaved>();
            
            Assert.Null(getFromCfg.Name);

            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Check_If_Setting_Is_Removed()
        {
            var cfg = new InMemoryConfiguration();
            var testClass = new TestClass() { Name = "TestRemove" };
            cfg.Set("testRemove", testClass);
            
            cfg.Remove("testRemove");
            
            Assert.False(cfg.Exist<TestClass>("testRemove"));

            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Check_If_Removing_Not_Saved_Setting_Does_Nothing()
        {
            var cfg = new InMemoryConfiguration();
            
            cfg.Remove("testRemove");
            
            Assert.False(cfg.Exist<TestClass>("testRemove"));

            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Check_For_Multiple_Threads_To_Write_In_Same_Field()
        {
            var cfg = new InMemoryConfiguration();

            var threads = new Thread[4];

            for (var i = 0; i < threads.Length; i++)
            {
                var j = i;
                threads[i] = new Thread(() => cfg.Set(new TestClass() { Name = $"TestMultiThread{j}" }));
                threads[i].Start();
            }
            
            for (var i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }
            
            Assert.Equal("TestMultiThread3", cfg.Get<TestClass>().Name);
            
            return Task.CompletedTask;
        }
        
        [Fact]
        public Task Check_For_Multiple_Threads_To_Write_In_Multiple_Fields()
        {
            var cfg = new InMemoryConfiguration();

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
            
            return Task.CompletedTask;
        }
    }
}