#nullable enable
using System;
using System.IO;
using Asv.IO;

namespace Asv.Mavlink;

public class AsvSdrListDataStoreFormat : GuidHierarchicalStoreFormat<IListDataFile<AsvSdrRecordFileMetadata>>
{
    public static readonly ListDataFileFormat FileFormat = new()
    {
        Version = "1.0.0",
        Type = "AsvSdrRecordFile",
        MetadataMaxSize =
            78 /*size of AsvSdrRecordPayload */ + sizeof(ushort) /* size of tag list */ +
            100 * 57 /* max 100 tag * size of AsvSdrRecordTagPayload */,
        ItemMaxSize = 256,
    };
    
    public AsvSdrListDataStoreFormat() : base(".sdr")
    {
    }
    public override IListDataFile<AsvSdrRecordFileMetadata> OpenFile(Stream stream)
    {
        return new ListDataFile<AsvSdrRecordFileMetadata>(stream, FileFormat, true);
    }

    public override IListDataFile<AsvSdrRecordFileMetadata> CreateFile(Stream stream, Guid id, string name)
    {
        var file = new ListDataFile<AsvSdrRecordFileMetadata>(stream, FileFormat, true);
        file.EditMetadata(metadata=>
        {
            
        });
        return file;
    }

    public override void Dispose()
    {
        
    }
}

public class AsvSdrStore : FileSystemHierarchicalStore<Guid, IListDataFile<AsvSdrRecordFileMetadata>>
{
    public static readonly IHierarchicalStoreFormat<Guid, IListDataFile<AsvSdrRecordFileMetadata>> StoreFormat =
        new AsvSdrListDataStoreFormat();
    
    public AsvSdrStore(string rootFolder, TimeSpan? fileCacheTime) : base(rootFolder, StoreFormat, fileCacheTime)
    {
        
    }
}

