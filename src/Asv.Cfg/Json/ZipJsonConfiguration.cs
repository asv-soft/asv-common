using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using Asv.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using ZLogger;

namespace Asv.Cfg.Json
{
    public class ZipJsonConfiguration:DisposableOnce, IConfiguration
    {
        private readonly ZipArchive _archive;
        private readonly object _sync = new();
        private readonly JsonSerializer _serializer;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private const string FixedFileExt = ".json";

        public ZipJsonConfiguration
        (
            Stream zipStream,
            bool leaveOpen = false, 
            ILogger? logger = null,
            IFileSystem? fileSystem = null
        )
        {
            ArgumentNullException.ThrowIfNull(zipStream);
            _logger = logger ?? NullLogger.Instance;
            _fileSystem = fileSystem ?? new FileSystem();
            _archive = new ZipArchive(zipStream, ZipArchiveMode.Update, leaveOpen);
            _serializer = JsonHelper.CreateDefaultJsonSerializer();
        }
        
        protected override void InternalDisposeOnce()
        {
            _logger.ZLogTrace($"Dispose ZipJsonConfiguration");
            lock (_sync)
            {
                _archive.Dispose();
            }
        }

        public IEnumerable<string> AvailableParts
        {
            get
            {
                lock (_sync)
                {
                    return _archive.Entries.Where(_ => _fileSystem.Path.GetExtension(_.Name) == FixedFileExt)
                        .Select(x => _fileSystem.Path.GetFileNameWithoutExtension(x.Name)).ToArray();
                }
            }
        }
        

        public bool Exist(string key)
        {
            ConfigurationHelper.ValidateKey(key);
            var fileName = GetFilePath(key);
            lock (_sync)
            {
                return _archive.Entries.Any(x => ConfigurationHelper.DefaultKeyComparer.Equals(x.Name, fileName));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetFilePath(string key)
        {
            return $"{key}{FixedFileExt}";
        }

        public TPocoType Get<TPocoType>(string key, Lazy<TPocoType> defaultValue)
        {
            ConfigurationHelper.ValidateKey(key);
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

        public void Set<TPocoType>(string key, TPocoType value)
        {
            ConfigurationHelper.ValidateKey(key);
            var fileName = GetFilePath(key);
            lock (_sync)
            {
                _logger.ZLogTrace($"Set configuration key [{key}]");
                _archive.GetEntry(fileName)?.Delete();
                var entry = _archive.CreateEntry(fileName);
                using var file = entry.Open();
                using var writer = new StreamWriter(file);
                using var wrt = new JsonTextWriter(writer);
                _serializer.Serialize(wrt, value);
            }
        }
        
        public void Remove(string key)
        {
            ConfigurationHelper.ValidateKey(key);
            var fileName = GetFilePath(key);
            lock (_sync)
            {
                _logger.ZLogTrace($"Remove configuration key [{key}]");
                var entry = _archive.GetEntry(fileName);
                entry?.Delete();
            }
        }
    }
}