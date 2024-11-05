using System;
using System.IO;
using Asv.Common;
using Newtonsoft.Json;

namespace Asv.Cfg
{
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

    public interface IVersionedFile:IConfiguration
    {
        SemVersion FileVersion { get; }
    }

    public abstract class ZipJsonVersionedFile: ZipJsonConfiguration,IVersionedFile
    {
        private readonly SemVersion _version;
        private const string InfoKey = "FileInfo";
        protected ZipJsonVersionedFile(Stream zipStream,SemVersion lastVersion, string fileType, bool createIfNotExist):base(zipStream)
        {
            var info = Get(InfoKey, new Lazy<ZipJsonFileInfo>(ZipJsonFileInfo.Empty));
            string type;
            if (info.Equals(ZipJsonFileInfo.Empty))
            {
                if (createIfNotExist)
                {
                    Set(InfoKey, new ZipJsonFileInfo(fileVersion: lastVersion.ToString(), fileType: fileType));
                    _version = lastVersion;
                    type = fileType;
                }
                else
                {
                    // TODO: add localization
                    throw new Exception("File version is empty.");
                }
            }
            else
            {
                if (SemVersion.TryParse(info.FileVersion, out _version) == false)
                {
                    throw new Exception($"Can't read file version. (Want 'X.X.X', got '{info.FileVersion}')");
                }
                if (_version > lastVersion)
                {
                    throw new Exception($"Unsupported file version. (Want '{lastVersion}', got '{_version}')");
                }
                type = info.FileType;
            }
            if (type.Equals(fileType, StringComparison.CurrentCultureIgnoreCase) == false)
            {
                throw new Exception($"Unsupported file type. (Want '{fileType}', got '{type}')");
            }
        }
    
        public SemVersion FileVersion => _version;
    }
}
