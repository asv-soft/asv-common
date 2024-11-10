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
using R3;
using ZLogger;

namespace Asv.Cfg
{
    public class ZipJsonConfiguration:DisposableOnce, IConfiguration
    {
        private readonly ZipArchive _archive;
        private readonly object _sync = new();
        private readonly JsonSerializer _serializer;
        private readonly ILogger _logger;
        private readonly Subject<ConfigurationException> _onError = new();
        private const string FixedFileExt = ".json";

        public ZipJsonConfiguration
        (
            Stream zipStream,
            bool leaveOpen = false, 
            ILogger? logger = null
        )
        {
            ArgumentNullException.ThrowIfNull(zipStream);
            _logger = logger ?? NullLogger.Instance;
            _archive = new ZipArchive(zipStream, ZipArchiveMode.Update, leaveOpen);
            _serializer = JsonHelper.CreateDefaultJsonSerializer();
        }
        
        protected override void InternalDisposeOnce()
        {
            _logger.ZLogTrace($"Dispose {this}");
            _onError.Dispose();
            lock (_sync)
            {
                _archive.Dispose();
            }
        }

        public override string ToString()
        {
            if (IsDisposed) return string.Empty;
            lock (_sync)
            {
                return $"{nameof(ZipJsonConfiguration)}:{_archive}";
            }
        }

        public IEnumerable<string> AvailableParts
        {
            get
            {
                lock (_sync)
                {
                    return _archive.Entries.Where(x => Path.GetExtension(x.Name) == FixedFileExt)
                        .Select(x => Path.GetFileNameWithoutExtension(x.Name)).ToArray();
                }
            }
        }
        

        public bool Exist(string key)
        {
            try
            {
                ConfigurationHelper.ValidateKey(key);
                var fileName = GetFilePath(key);
                lock (_sync)
                {
                    return _archive.Entries.Any(x => ConfigurationHelper.DefaultKeyComparer.Equals(x.Name, fileName));
                }
            }
            catch (Exception e)
            {
                _logger.ZLogError(e,$"Error to check exist '{key}' part:{e.Message}");
                var ex = new ConfigurationException($"Error to check exist '{key}' part",e);
                _onError.OnNext(ex);
                throw ex;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetFilePath(string key) => $"{key}{FixedFileExt}";

        public TPocoType Get<TPocoType>(string key, Lazy<TPocoType> defaultValue)
        {
            try
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
            catch (Exception e)
            {
                _logger.ZLogError(e,$"Error to get '{key}' part:{e.Message}");
                var ex = new ConfigurationException($"Error to get '{key}' part",e);
                _onError.OnNext(ex);
                throw ex;
            }
        }

        public void Set<TPocoType>(string key, TPocoType value)
        {
            try
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
            catch (Exception e)
            {
                _logger.ZLogError(e,$"Error to set '{key}' part:{e.Message}");
                var ex = new ConfigurationException($"Error to set '{key}' part",e);
                _onError.OnNext(ex);
                throw ex;
            }
        }
        
        public void Remove(string key)
        {
            try
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
            catch (Exception e)
            {
                _logger.ZLogError(e,$"Error to remove '{key}' part:{e.Message}");
                var ex = new ConfigurationException($"Error to remove '{key}' part",e);
                _onError.OnNext(ex);
                throw ex;
            }
        }

        public Observable<ConfigurationException> OnError => _onError;
    }
}