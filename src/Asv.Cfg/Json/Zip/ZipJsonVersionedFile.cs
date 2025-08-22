using System;
using System.Collections.Generic;
using System.IO;
using Asv.Common;
using Microsoft.Extensions.Logging;

namespace Asv.Cfg
{
    public class ZipJsonVersionedFile: ZipJsonConfiguration,IVersionedFile
    {
        private readonly SemVersion _version;
        private const string InfoKey = "FileInfo";

        public ZipJsonVersionedFile(
            Stream stream,
            SemVersion lastVersion, 
            string fileType, 
            bool createIfNotExist,
            bool leaveOpen = false, 
            ILogger? logger = null )
            :base(stream, leaveOpen,logger)
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
                    throw new ConfigurationException("File version is empty.");
                }
            }
            else
            {
                if (SemVersion.TryParse(info.FileVersion, out var version) == false)
                {
                    throw new ConfigurationException($"Can't read file version. (Want 'X.X.X', got '{info.FileVersion}')");
                }

                _version = version ?? throw new InvalidOperationException();
                if (_version > lastVersion)
                {
                    throw new ConfigurationException($"Unsupported file version. (Want '{lastVersion}', got '{_version}')");
                }
                type = info.FileType;
            }
            if (type.Equals(fileType, StringComparison.CurrentCultureIgnoreCase) == false)
            {
                throw new ConfigurationException($"Unsupported file type. (Want '{fileType}', got '{type}')");
            }
        }

        protected override IEnumerable<string> InternalSafeGetReservedParts()
        {
            yield return InfoKey;
        }

        public SemVersion FileVersion => _version;
    }
}
