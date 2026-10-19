using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using ZLogger;

namespace Asv.Cfg
{
    /// <summary>
    /// Stores configuration values as JSON entries inside a ZIP archive.
    /// </summary>
    public class ZipJsonConfiguration : ConfigurationBase
    {
        private readonly ZipArchive _archive;
        private readonly Lock _sync = new();
        private readonly JsonSerializer _serializer;
        private readonly ILogger _logger;
        private const string FixedFileExt = ".json";

        /// <summary>
        /// Initializes a new instance of the <see cref="ZipJsonConfiguration"/> class.
        /// </summary>
        /// <param name="stream">The ZIP archive stream.</param>
        /// <param name="leaveOpen">A value indicating whether the stream remains open after disposal.</param>
        /// <param name="logger">The optional logger.</param>
        public ZipJsonConfiguration(Stream stream, bool leaveOpen = false, ILogger? logger = null)
        {
            ArgumentNullException.ThrowIfNull(stream);
            _logger = logger ?? NullLogger.Instance;
            _archive = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen);
            _serializer = JsonHelper.CreateDefaultJsonSerializer();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (IsDisposed)
            {
                return string.Empty;
            }

            using (_sync.EnterScope())
            {
                return $"{nameof(ZipJsonConfiguration)}:{_archive}";
            }
        }

        /// <inheritdoc />
        protected override IEnumerable<string> InternalSafeGetReservedParts()
        {
            return [];
        }

        /// <inheritdoc />
        protected override IEnumerable<string> InternalSafeGetAvailableParts()
        {
            using (_sync.EnterScope())
            {
                var keys = new HashSet<string>(ConfigurationMixin.DefaultKeyComparer);
                foreach (var entry in _archive.Entries.Where(x =>
                    ConfigurationMixin.DefaultKeyComparer.Equals(Path.GetExtension(x.Name), FixedFileExt)))
                {
                    var key = Path.GetFileNameWithoutExtension(entry.Name);
                    if (!keys.Add(key))
                    {
                        throw new ConfigurationException($"Conflicting configuration key: '{key}'.");
                    }
                }
                return keys.ToArray();
            }
        }

        /// <inheritdoc />
        protected override bool InternalSafeExist(string key)
        {
            var fileName = GetFilePath(key);
            using (_sync.EnterScope())
            {
                return FindEntry(fileName) is not null;
            }
        }

        // The caller holds _sync while resolving and modifying entries.
        private ZipArchiveEntry? FindEntry(string fileName)
        {
            ZipArchiveEntry? match = null;
            foreach (var entry in _archive.Entries)
            {
                if (!ConfigurationMixin.DefaultKeyComparer.Equals(entry.Name, fileName))
                {
                    continue;
                }
                if (match is not null)
                {
                    throw new ConfigurationException($"Conflicting configuration entries: '{match.FullName}' and '{entry.FullName}'.");
                }
                match = entry;
            }
            return match;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetFilePath(string key) => $"{key}{FixedFileExt}";

        /// <inheritdoc />
        protected override TPocoType InternalSafeGet<TPocoType>(
            string key,
            Lazy<TPocoType> defaultValue
        )
        {
            var fileName = GetFilePath(key);
            using (_sync.EnterScope())
            {
                var entry = FindEntry(fileName);
                if (entry == null)
                {
                    _logger.ZLogTrace(
                        $"Configuration key [{key}] not found. Create new with default value"
                    );
                    var inst = defaultValue.Value;
                    var stream2 = _archive.CreateEntry(fileName);
                    using var file = stream2.Open();
                    using var writer = new StreamWriter(file);
                    using var wrt = new JsonTextWriter(writer);
                    _serializer.Serialize(wrt, inst);
                    return inst;
                }

                using var stream = entry.Open();
                using var streamReader = new StreamReader(stream);
                using var rdr = new JsonTextReader(streamReader);
                return _serializer.Deserialize<TPocoType>(rdr)
                    ?? throw new InvalidOperationException();
            }
        }

        /// <inheritdoc />
        protected override void InternalSafeSave<TPocoType>(string key, TPocoType value)
        {
            var fileName = GetFilePath(key);
            using (_sync.EnterScope())
            {
                var existing = FindEntry(fileName);
                var storedName = existing?.FullName ?? fileName;
                existing?.Delete();
                var entry = _archive.CreateEntry(storedName);
                using var file = entry.Open();
                using var writer = new StreamWriter(file);
                using var wrt = new JsonTextWriter(writer);
                _serializer.Serialize(wrt, value);
            }
        }

        /// <inheritdoc />
        protected override void InternalSafeRemove(string key)
        {
            var fileName = GetFilePath(key);
            using (_sync.EnterScope())
            {
                _logger.ZLogTrace($"Remove configuration key [{key}]");
                var entry = FindEntry(fileName);
                entry?.Delete();
            }
        }

        #region Dispose

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                using (_sync.EnterScope())
                {
                    _archive.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        /// <inheritdoc />
        protected override async ValueTask DisposeAsyncCore()
        {
            using (_sync.EnterScope())
            {
                _archive.Dispose();
            }

            await base.DisposeAsyncCore();
        }

        #endregion
    }
}
