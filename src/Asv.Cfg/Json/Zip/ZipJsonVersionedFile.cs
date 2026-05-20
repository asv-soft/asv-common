using System;
using System.Collections.Generic;
using System.IO;
using Asv.Common;
using Microsoft.Extensions.Logging;

namespace Asv.Cfg
{
    /// <summary>
    /// Stores versioned configuration values as JSON entries inside a ZIP archive.
    /// </summary>
    public class ZipJsonVersionedFile : ZipJsonConfiguration, IVersionedFile
    {
        private readonly SemVersion _version;
        private const string InfoKey = "FileInfo";

        /// <summary>
        /// Initializes a new instance of the <see cref="ZipJsonVersionedFile"/> class.
        /// </summary>
        /// <param name="stream">The ZIP archive stream.</param>
        /// <param name="lastVersion">The latest supported file version.</param>
        /// <param name="fileType">The expected file type.</param>
        /// <param name="createIfNotExist">A value indicating whether metadata is created when missing.</param>
        /// <param name="leaveOpen">A value indicating whether the stream remains open after disposal.</param>
        /// <param name="logger">The optional logger.</param>
        public ZipJsonVersionedFile(
            Stream stream,
            SemVersion lastVersion,
            string fileType,
            bool createIfNotExist,
            bool leaveOpen = false,
            ILogger? logger = null
        )
            : base(stream, leaveOpen, logger)
        {
            var info = Get(InfoKey, new Lazy<ZipJsonFileInfo>(ZipJsonFileInfo.Empty));
            string type;
            if (info.Equals(ZipJsonFileInfo.Empty))
            {
                if (createIfNotExist)
                {
                    Set(
                        InfoKey,
                        new ZipJsonFileInfo(fileVersion: lastVersion.ToString(), fileType: fileType)
                    );
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
                    throw new ConfigurationException(
                        $"Can't read file version. (Want 'X.X.X', got '{info.FileVersion}')"
                    );
                }

                _version = version ?? throw new InvalidOperationException();
                if (_version > lastVersion)
                {
                    throw new ConfigurationException(
                        $"Unsupported file version. (Want '{lastVersion}', got '{_version}')"
                    );
                }
                type = info.FileType;
            }
            if (type.Equals(fileType, StringComparison.CurrentCultureIgnoreCase) == false)
            {
                throw new ConfigurationException(
                    $"Unsupported file type. (Want '{fileType}', got '{type}')"
                );
            }
        }

        /// <inheritdoc />
        protected override IEnumerable<string> InternalSafeGetReservedParts()
        {
            yield return InfoKey;
        }

        /// <inheritdoc />
        public SemVersion FileVersion => _version;
    }
}
