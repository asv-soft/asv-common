using System;
using System.Linq;
using System.Threading;
using AutoFixture;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace Asv.Cfg.Test;

public class CustomTestClass()
    : ICustomConfigurable
{
    [CanBeNull] 
    public string Value { get; set; }
    
    public void Load(string key, IConfiguration configuration)
    {
        Value = configuration.Get("KEY",new Lazy<string>());
    }

    public void Save(string key, IConfiguration configuration)
    {
        configuration.Set("KEY",Value);
    }
}

public abstract class ConfigurationBaseTest<T>(ITestOutputHelper log)
    where T : IConfiguration
{
    protected abstract IDisposable CreateForTest(out T configuration);

    public TestLoggerFactory LogFactory { get; set; } = new(log, TimeProvider.System, "Test");

    [Fact]
    public void Check_If_Saved_Setting_Exists()
    {
        using var cleanup = CreateForTest(out var cfg);
        var original = new TestClassWithEnums() { Name = "Test", Enum = EnumTest.Test1 };
        cfg.Set("test",original );

        Assert.True(cfg.Exist("test"));
        var actualResult = cfg.Get<TestClassWithEnums>("test");
        Assert.Equal(original.Name, actualResult.Name);
        Assert.Equal(original.Enum, actualResult.Enum);
    }
    
    [Fact]
    public void Check_CustomSaveInterface_Success()
    {
        using var cleanup = CreateForTest(out var cfg);

        // Arrange
        var fixture = new Fixture();
        var origin = new CustomTestClass
        {
            Value = fixture.Create<string>(),
        };
        cfg.Set(origin);

        var actualResult = cfg.Get<CustomTestClass>("test");
        Assert.Equal(origin.Value, actualResult.Value);
    }
    
     [Fact]
     public void Check_If_Not_Saved_Setting_Exists()
     {
         using var cleanup = CreateForTest(out var cfg);
         cfg.Set("test", new TestClass(){ Name = "Test" });

         Assert.False(cfg.Exist("no_test"));
     }
        
     [Fact]
     public void Check_If_Multiple_Copies_Of_Same_Setting_Are_Saved()
     { 
         using var cleanup = CreateForTest(out var cfg); 
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
     }
        
     [Fact]
     public void Check_Available_Parts_Value()
     {
         using var cleanup = CreateForTest(out var cfg);
         cfg.Set("test1", new TestClass(){ Name = "Test1" });
         Thread.Sleep(50);
         cfg.Set("test2", new TestClass(){ Name = "Test2" });
         Thread.Sleep(50);
         cfg.Set("test3", new TestClass(){ Name = "Test3" });
         Thread.Sleep(50);
         cfg.Set("test4", new TestClass(){ Name = "Test4" });
         Thread.Sleep(50);
            
         var expectedResult = new string[] { "test1", "test2", "test3", "test4" };
         var exclude = cfg.ReservedParts.ToHashSet();
         var actualResult = cfg.AvailableParts.OrderBy(x=>x).Where(x=>exclude.Contains(x) == false).ToArray(); // items can be reordered in any way, so we need to sort them 
            
         Assert.Equal(expectedResult, actualResult);
     }

     [Fact]
     public void Check_If_Saved_Setting_Is_Returned_Correctly()
     {
         using var cleanup = CreateForTest(out var cfg);
         var testClass = new TestClass() { Name = "TestGet" };
         cfg.Set(testClass);

         var getFromCfg = cfg.Get<TestClass>();
            
         Assert.Equal(testClass.Name, getFromCfg.Name);
     }
        
     [Fact]
     public void Check_If_Not_Saved_Setting_Is_Returned_With_Default_Value()
     {
         using var cleanup = CreateForTest(out var cfg);
            
         var getFromCfg = cfg.Get<TestNotSaved>();
            
         Assert.Null(getFromCfg.Name);
     }
        
     [Fact]
     public void Check_If_Setting_Is_Removed()
     {
         using var cleanup = CreateForTest(out var cfg);
         var testClass = new TestClass() { Name = "TestRemove" };
         cfg.Set("testRemove", testClass);
            
         cfg.Remove("testRemove");
            
         Assert.False(cfg.Exist("testRemove"));
     }
        
     [Fact]
     public void Check_If_Removing_Not_Saved_Setting_Does_Nothing()
     {
         using var cleanup = CreateForTest(out var cfg);
            
         cfg.Remove("testRemove");
            
         Assert.False(cfg.Exist("testRemove"));
     }
        
     [Fact]
     public void Check_For_Multiple_Threads_To_Write_In_Same_Field()
     {
         using var cleanup = CreateForTest(out var cfg);

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
         // The starting order of threads is chosen by the OS at its discretion and may not coincide with the code.
         //Assert.Equal("TestMultiThread3", cfg.Get<TestClass>().Name);
            
         // just check that all threads are finished without exception
     }
        
     [Fact]
     public void Check_For_Multiple_Threads_To_Write_In_Multiple_Fields()
     {
         using var cleanup = CreateForTest(out var cfg);

         var threads = new Thread[4];

         threads[0] = new Thread(() => cfg.Set("one", new TestMultiThreadClassOne() { Name = "First" }));
         threads[1] = new Thread(() => cfg.Set("two", new TestMultiThreadClassTwo() { Name = "Second" }));
         threads[2] = new Thread(() => cfg.Set("three", new TestMultiThreadClassThree() { Name = "Third" }));
         threads[3] = new Thread(() => cfg.Set("four", new TestMultiThreadClassFour() { Name = "Fourth" }));

         foreach (var thread in threads)
         {
             thread.Start();
         }

         foreach (var thread in threads)
         {
             thread.Join();
         }

         Assert.True(cfg.Exist("one"));
         Assert.True(cfg.Exist("two"));
         Assert.True(cfg.Exist("three"));
         Assert.True(cfg.Exist("four"));
     }
}