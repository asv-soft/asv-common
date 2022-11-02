using System;
using System.Collections.Generic;
using System.IO;

namespace Asv.Store
{
    public interface IFileInfo
    {
        string GridName { get; }
        long Length { get; }
        DateTime UploadDate { get; }
        string Id { get; }
        string Filename { get; }
        string MimeType { get; }
        string this[string key] { get;}
    }

    public interface IFileGrid
    {
        string GridName { get; }
        IEnumerable<IFileInfo> Files { get; }
        bool Exist(string fileName);
        IFileInfo Upload(string fileName, Stream stream);
        IFileInfo Download(string fileName, Stream stream);
        IFileInfo GetInfo(string fileName);
        void UpdateMetadata(string fileName, IReadOnlyDictionary<string,string> metadata);
        bool Delete(string fileName);
    }
}
