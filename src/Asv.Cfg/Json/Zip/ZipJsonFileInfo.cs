using System;
using Newtonsoft.Json;

namespace Asv.Cfg;

/// <summary>
/// Describes metadata stored in a versioned ZIP JSON configuration file.
/// </summary>
/// <param name="fileVersion">The file version.</param>
/// <param name="fileType">The file type.</param>
[method: JsonConstructor]
public readonly struct ZipJsonFileInfo(string fileVersion, string fileType)
    : IEquatable<ZipJsonFileInfo>
{
    /// <summary>
    /// Gets an empty file info value.
    /// </summary>
    public static ZipJsonFileInfo Empty { get; } = new(string.Empty, string.Empty);

    /// <summary>
    /// Gets the file version.
    /// </summary>
    public string FileVersion { get; } = fileVersion;

    /// <summary>
    /// Gets the file type.
    /// </summary>
    public string FileType { get; } = fileType;

    /// <inheritdoc />
    public bool Equals(ZipJsonFileInfo other)
    {
        return FileVersion == other.FileVersion && FileType == other.FileType;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ZipJsonFileInfo other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(FileVersion, FileType);
    }

    /// <summary>
    /// Determines whether two file info values are equal.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(ZipJsonFileInfo left, ZipJsonFileInfo right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two file info values are not equal.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true"/> if the values are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(ZipJsonFileInfo left, ZipJsonFileInfo right)
    {
        return !left.Equals(right);
    }
}
