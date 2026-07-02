#nullable enable

namespace Asv.Store;

public interface IFileStorePart
{
    IEnumerable<IStoredFile> Enumerate(string? relativeDirectory = null, bool recursive = true);

    IStoredFile? Get(string relativePath);

    bool Exists(string relativePath);

    void Write(
        string relativePath,
        Stream content,
        string contentType = "application/octet-stream",
        IReadOnlyDictionary<string, string>? metadata = null
    );

    bool Delete(string relativePath);
}

public interface IStoredFile
{
    string RelativePath { get; }
    string Name { get; }
    string ContentType { get; }
    IReadOnlyDictionary<string, string> Metadata { get; }
    long Length { get; }
    Stream OpenRead();
}
