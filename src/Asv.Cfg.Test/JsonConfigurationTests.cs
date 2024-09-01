using System;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Asv.Cfg.Json;
using Xunit;
using Xunit.Abstractions;

namespace Asv.Cfg.Test
{
    
    public enum EnumTest
    {
        Test1 = 1,
        Test2 = 2,
        Test3 = 3
    }
    #region Test Classes
    public class TestNotSaved
    {
        public string Name { get; set; }
    }
    
    public class TestClass
    {
        public string Name { get; set; }
    }
    
    public class TestClassWithEnums
    {
        public string Name { get; set; }
        public EnumTest Enum { get; set; }
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
    
    public class JsonConfigurationTests:ConfigurationTestBase<JsonConfiguration>
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public JsonConfigurationTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public override IDisposable CreateForTest(out JsonConfiguration configuration)
        {
            var workingDir = $"{Path.GetTempPath()}\\{Path.GetRandomFileName()}";
            if (Directory.Exists(workingDir))
            {
                Directory.Delete(workingDir);
            }
            _testOutputHelper.WriteLine($"Working directory: {workingDir}");
            configuration = new JsonConfiguration(workingDir);
            var cfg = configuration;
            return Disposable.Create(() =>
            {
                cfg.Dispose();
                Directory.Delete(workingDir, true);
            });
        }
        
        [Theory]
        [InlineData("")]
        [InlineData("    ")]
        public void Configuration_Should_Throw_Argument_Exception_If_FolderPath_Is_Empty(string folderPath)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var configuration = new JsonConfiguration(folderPath);
            });
            
            
        }
        
        [Theory]
        [InlineData(null)]
        public void Configuration_Should_Throw_Argument_Exception_If_FolderPath_Is_Null(string folderPath)
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var configuration = new JsonConfiguration(folderPath);
            });
            
            
        }

        [Fact]
        public void Configuration_Should_Be_Saved_In_Set_Directory()
        {
            using var cleanup = CreateForTest(out var cfg);
            
            cfg.Set(new TestClass(){ Name = "Test" });
            var fileName = Directory.GetFiles(cfg.WorkingFolder, "*.json");
            
            Assert.Equal(Path.Combine(cfg.WorkingFolder,"TestClass.json"), fileName.FirstOrDefault());

            
        }
      

       

        
        [Fact]
        public void Check_If_Multiple_Copies_Of_Same_Setting_Are_Saved_In_Directory()
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

            var dirInfo = new DirectoryInfo(cfg.WorkingFolder);
            var files = dirInfo.GetFiles();

            var fileQuery =
                from file in files
                where file.Name == "TestClass.json"
                select file;

            var fileInfos = fileQuery as FileInfo[] ?? fileQuery.ToArray();

            Assert.Single(fileInfos);

            
        }
        
        
        
        
       
    }
}