using System;
using Newtonsoft.Json;

namespace Asv.Cfg;

[method: JsonConstructor]
public readonly struct ZipJsonFileInfo(string fileVersion, string fileType) : IEquatable<ZipJsonFileInfo>
{
    public static ZipJsonFileInfo Empty { get; } = new(string.Empty, string.Empty);
    public string FileVersion { get; } = fileVersion;
    public string FileType { get; } = fileType;

    public bool Equals(ZipJsonFileInfo other)
    {
        return FileVersion == other.FileVersion && FileType == other.FileType;
    }

    public override bool Equals(object? obj)
    {
        return obj is ZipJsonFileInfo other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FileVersion, FileType);
    }

    public static bool operator ==(ZipJsonFileInfo left, ZipJsonFileInfo right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ZipJsonFileInfo left, ZipJsonFileInfo right)
    {
        return !left.Equals(right);
    }
}