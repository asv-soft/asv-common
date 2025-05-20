using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using ZLogger;

namespace Asv.Cfg
{
    public class ZipJsonConfiguration:ConfigurationBase
    {
        private readonly ZipArchive _archive;
        private readonly object _sync = new();
        private readonly JsonSerializer _serializer;
        private readonly ILogger _logger;
        private const string FixedFileExt = ".json";

        public ZipJsonConfiguration
        (
            Stream stream,
            bool leaveOpen = false, 
            ILogger? logger = null
        )
        {
            ArgumentNullException.ThrowIfNull(stream);
            _logger = logger ?? NullLogger.Instance;
            _archive = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen);
            _serializer = JsonHelper.CreateDefaultJsonSerializer();
        }

        public override string ToString()
        {
            if (IsDisposed) return string.Empty;
            lock (_sync)
            {
                return $"{nameof(ZipJsonConfiguration)}:{_archive}";
            }
        }

        protected override IEnumerable<string> InternalSafeGetReservedParts()
        {
            return [];
        }

        protected override IEnumerable<string> InternalSafeGetAvailableParts()
        {
            lock (_sync)
            {
                return _archive.Entries.Where(x => Path.GetExtension(x.Name) == FixedFileExt)
                    .Select(x => Path.GetFileNameWithoutExtension(x.Name)).ToImmutableArray();
            }
        }

        protected override bool InternalSafeExist(string key)
        {
            var fileName = GetFilePath(key);
            lock (_sync)
            {
                return _archive.Entries.Any(x => ConfigurationHelper.DefaultKeyComparer.Equals(x.Name, fileName));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetFilePath(string key) => $"{key}{FixedFileExt}";

        protected override TPocoType InternalSafeGet<TPocoType>(string key, Lazy<TPocoType> defaultValue)
        {
            var fileName = GetFilePath(key);
            lock (_sync)
            {
                var entry = _archive.Entries.FirstOrDefault(x => ConfigurationHelper.DefaultKeyComparer.Equals(x.Name, fileName));
                if (entry == default)
                {
                    _logger.ZLogTrace($"Configuration key [{key}] not found. Create new with default value");
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
                return _serializer.Deserialize<TPocoType>(rdr) ?? throw new InvalidOperationException();
            }
        }


        protected override void InternalSafeSave<TPocoType>(string key, TPocoType value)
        {
            var fileName = GetFilePath(key);
            lock (_sync)
            {
                _archive.GetEntry(fileName)?.Delete();
                var entry = _archive.CreateEntry(fileName);
                using var file = entry.Open();
                using var writer = new StreamWriter(file);
                using var wrt = new JsonTextWriter(writer);
                _serializer.Serialize(wrt, value);
            }
        }


        protected override void InternalSafeRemove(string key)
        {
            var fileName = GetFilePath(key);
            lock (_sync)
            {
                _logger.ZLogTrace($"Remove configuration key [{key}]");
                var entry = _archive.GetEntry(fileName);
                entry?.Delete();
            }
        }

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_sync)
                {
                    _archive.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            lock (_sync)
            {
                _archive.Dispose();
            }

            await base.DisposeAsyncCore();
        }

        #endregion
    }
}