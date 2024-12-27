using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using Asv.IO.Test.HierarchicalStore;
using Xunit;

namespace Asv.IO.Test.ListDataFile;

public class ListDataFileTests
{
    private static MockFileSystem _fileSystem = new();

    public static readonly ListDataFileFormat FileFormat1 = new()
    {
        Version = "1.0.0",
        Type = "TestFile1",
        MetadataMaxSize = 1024 * 4,
        ItemMaxSize = 256
    };

    public static readonly ListDataFileFormat FileFormat2 = new()
    {
        Version = "1.0.0",
        Type = "TestFile2",
        MetadataMaxSize = 1024 * 4,
        ItemMaxSize = 256
    };

    [Fact]
    public void Ctor_Null_Reference_Fail()
    {
        using var strm = new MemoryStream();
        Assert.Throws<ArgumentNullException>(() =>
        {
            using var file = new ListDataFile<AsvSdrRecordFileMetadata>(null, FileFormat1, false, _fileSystem);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            using var file = new ListDataFile<AsvSdrRecordFileMetadata>(new MemoryStream(), null, false, _fileSystem);
        });
    }

    

    [Fact]
    public static void EditMetadata_Null_Argument_Fail()
    {
        using var strm = new MemoryStream();
        IListDataFile<AsvSdrRecordFileMetadata> file =
            new ListDataFile<AsvSdrRecordFileMetadata>(strm, FileFormat1, false, _fileSystem);

        Assert.Throws<NullReferenceException>(() => { file.EditMetadata(null); });
    }

    [Fact]
    public static void Write_Null_Argument_Fail()
    {
        using var strm = new MemoryStream();
        IListDataFile<AsvSdrRecordFileMetadata> file =
            new ListDataFile<AsvSdrRecordFileMetadata>(strm, FileFormat1, false, _fileSystem);

        Assert.Throws<NullReferenceException>(() => { file.Write(0, null); });
    }

    [Fact]
    public static void Different_Headers_Read_Fail()
    {
        using var strm = new MemoryStream();

        var file1 = new ListDataFile<AsvSdrRecordFileMetadata>(strm, FileFormat1, false, _fileSystem);

        file1.Dispose();

        Assert.Throws<Exception>(() =>
        {
            var file2 = new ListDataFile<AsvSdrRecordFileMetadata>(strm, FileFormat2, false, _fileSystem);
        });
    }

    [Fact]
    public static void Wrong_Version_Header_Fail()
    {
        using var strm = new MemoryStream();
        Assert.Throws<ArgumentException>(() =>
        {
            var format = new ListDataFileFormat()
            {
                Version = "aboba",
                Type = "type",
                MetadataMaxSize = 1234,
                ItemMaxSize = 567
            };
        });
    }

    [Fact]
    public static void Null_Version_Header_Fail()
    {
        using var strm = new MemoryStream();

        var format = new ListDataFileFormat()
        {
            Version = null,
            Type = "type",
            MetadataMaxSize = 1234,
            ItemMaxSize = 567
        };

        Assert.Throws<InvalidOperationException>(() =>
        {
            using var file = new ListDataFile<AsvSdrRecordFileMetadata>(strm, format, false, _fileSystem);
        });
    }

    [Fact]
    public static void Empty_Type_Header_Fail()
    {
        using var strm = new MemoryStream();

        var format = new ListDataFileFormat()
        {
            Version = "1.0.0",
            Type = "",
            MetadataMaxSize = 1234,
            ItemMaxSize = 567
        };

        Assert.Throws<InvalidOperationException>(() =>
        {
            using var file = new ListDataFile<AsvSdrRecordFileMetadata>(strm, format, false, _fileSystem);
        });
    }

    [Fact]
    public static void Null_Type_Header_Fail()
    {
        using var strm = new MemoryStream();

        var format = new ListDataFileFormat()
        {
            Version = "1.0.0",
            Type = null,
            MetadataMaxSize = 1234,
            ItemMaxSize = 567
        };

        Assert.Throws<InvalidOperationException>(() =>
        {
            using var file = new ListDataFile<AsvSdrRecordFileMetadata>(strm, format, false, _fileSystem);
        });
    }

    [Theory]
    [InlineData("1.0.0", "type", 0, 53)]
    [InlineData("1.0.0", "type", 1241, 0)]
    public static void Max_Size_Header_Fail(string version, string type, ushort metadataMaxSize, ushort itemMaxSize)
    {
        using var strm = new MemoryStream();

        var format = new ListDataFileFormat()
        {
            Version = version,
            Type = type,
            MetadataMaxSize = metadataMaxSize,
            ItemMaxSize = itemMaxSize
        };

        Assert.Throws<InvalidOperationException>(() =>
        {
            using var file = new ListDataFile<AsvSdrRecordFileMetadata>(strm, format, false, _fileSystem);
        });
    }

    
}