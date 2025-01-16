using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using R3;
using ZLogger;

namespace Asv.Cfg
{
    
    public class JsonOneFileConfiguration : ConfigurationBase
    {
        private readonly string _fileName;
        private readonly string _backupFileName;
        private readonly bool _sortKeysInFile;
        private readonly ConcurrentDictionary<string, JToken> _values;
        private readonly Subject<Unit> _onNeedToSave = new();
        private readonly IDisposable _saveSubscribe;
        private readonly object _sync = new();
        private readonly bool _deferredFlush;
        private readonly JsonSerializer _serializer;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;


        public JsonOneFileConfiguration(
            string fileName, 
            bool createIfNotExist, 
            TimeSpan? flushToFileDelayMs, 
            bool sortKeysInFile = false, 
            ILogger? logger = null,
            IFileSystem? fileSystem = null,
            TimeProvider? timeProvider = null
        )
        {
            _fileSystem= fileSystem ?? new FileSystem();
            _fileName = _fileSystem.Path.GetFullPath(fileName);
            _backupFileName = _fileName + ".backup";
            _sortKeysInFile = sortKeysInFile;
            _logger = logger ?? NullLogger.Instance; 
            timeProvider ??= TimeProvider.System;
            _serializer = JsonHelper.CreateDefaultJsonSerializer();
            _serializer.Converters.Add(new StringEnumConverter());
            

            if (flushToFileDelayMs == null)
            {
                _logger.ZLogDebug($"{fileName} create:{createIfNotExist} flush: No, write immediately");
                _deferredFlush = false;
            }
            else
            {
                _deferredFlush = true;
                _logger.ZLogDebug($"{fileName} create:{createIfNotExist} flush: every {flushToFileDelayMs.Value.TotalSeconds:F2} seconds");
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

            var dir = _fileSystem.Path.GetDirectoryName(_fileSystem.Path.GetFullPath(fileName));
            ArgumentException.ThrowIfNullOrWhiteSpace(dir);

            _saveSubscribe = flushToFileDelayMs == null
                ? _onNeedToSave.Subscribe(InternalSaveChanges)
                : _onNeedToSave.ThrottleLast(flushToFileDelayMs.Value,timeProvider).Subscribe(InternalSaveChanges);

            if (_fileSystem.Directory.Exists(dir) == false)
            {
                if (!createIfNotExist)
                    throw new DirectoryNotFoundException($"Directory with config file not exist: {dir}");
                
                _logger.ZLogWarning($"Directory with config file not exist. Try to create it: {dir}");
                _fileSystem.Directory.CreateDirectory(dir);
            }
            if (_fileSystem.File.Exists(fileName) == false && _fileSystem.File.Exists(_backupFileName))
            {
                _logger.ZLogWarning($"Configuration file doesn't exist. Try to load from backup file: {_backupFileName} => {_fileName}");
                _fileSystem.File.Replace(_backupFileName,_fileName,null,true);
            }
            
            if (_fileSystem.File.Exists(fileName) == false)
            {
               
                if (createIfNotExist)
                {
                    _logger.ZLogWarning($"Config file not exist. Try to create {fileName}");
                    _values = new ConcurrentDictionary<string, JToken>(ConfigurationHelper.DefaultKeyComparer);
                    InternalSaveChanges(Unit.Default);
                }
                else
                {
                    throw InternalPublishError(new ConfigurationException($"Configuration file not exist {fileName}"));
                }
            }
            else
            {
                using var text = _fileSystem.File.OpenRead(_fileName);
                try
                {
                    using var reader = new JsonTextReader(new StreamReader(text));
                    _values = new ConcurrentDictionary<string, JToken>(ConfigurationHelper.DefaultKeyComparer);
                    _serializer.Populate(reader, _values);
                }
                catch (Exception e)
                {
                    throw InternalPublishError(new ConfigurationException($"Error to load JSON configuration from file. File content: {text}",e));
                }
            }
        }
        
        public string FileName => _fileName;
        
        private void InternalSaveChanges(Unit unit)
        {
            lock (_sync)
            {
                try
                {
                    if (_fileSystem.File.Exists(_backupFileName))
                    {
                        _fileSystem.File.Delete(_backupFileName);
                    }
                    using (var file = _fileSystem.File.CreateText(_backupFileName))
                    {
                        //serialize object directly into file stream
                        if (_sortKeysInFile)
                        {
                            _serializer.Serialize(file, new SortedDictionary<string, JToken>(_values));
                        }
                        else
                        {
                            _serializer.Serialize(file, _values);     
                        }
                    }
                    _fileSystem.File.Move(_backupFileName,_fileName,true);
                    _logger.ZLogTrace($"Flush configuration to file '{_fileName}' [{_values.Count} items] ");
                }
                catch (Exception e)
                {
                    var ex = InternalPublishError(new ConfigurationException($"Error to serialize configuration and save it to file ({_fileName})",e));
                    if (_deferredFlush == false) throw ex;
                }
            }
        }

        protected override IEnumerable<string> InternalSafeGetReservedParts() => Array.Empty<string>();

        protected override IEnumerable<string> InternalSafeGetAvailableParts() => _values.Keys;

        protected override bool InternalSafeExist(string key) => _values.ContainsKey(key);

        protected override TPocoType InternalSafeGet<TPocoType>(string key, Lazy<TPocoType> defaultValue)
        {
            if (_values.TryGetValue(key, out var value))
            {
                return value.ToObject<TPocoType>() ?? throw new InvalidOperationException();
            }

            var inst = defaultValue.Value;
            Set(key,inst);
            return inst;
        }

        protected override void InternalSafeSave<TPocoType>(string key, TPocoType value)
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            using var jsonTextWriter = new JsonTextWriter(writer);
            jsonTextWriter.Formatting = _serializer.Formatting;
            _serializer.Serialize(jsonTextWriter, value, typeof(TPocoType));
            writer.Flush();          
            stream.Position = 0;
            using var jsonReader = new JsonTextReader(new StreamReader(stream));
            var jValue = _serializer.Deserialize<JToken>(jsonReader) ?? throw new InvalidOperationException();
            _values.AddOrUpdate(key,jValue , (_, _) => jValue);
            _onNeedToSave.OnNext(Unit.Default);
        }

        protected override void InternalSafeRemove(string key)
        {
            if (_values.TryRemove(key, out _))
            {
                _onNeedToSave.OnNext(Unit.Default);
            }
        }

        public override string ToString()
        {
            return $"{nameof(JsonOneFileConfiguration)}[{_fileName}]";
        }

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _onNeedToSave.Dispose();
                _saveSubscribe.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            await CastAndDispose(_onNeedToSave);
            await CastAndDispose(_saveSubscribe);

            await base.DisposeAsyncCore();

            return;

            static async ValueTask CastAndDispose(IDisposable resource)
            {
                if (resource is IAsyncDisposable resourceAsyncDisposable)
                    await resourceAsyncDisposable.DisposeAsync();
                else
                    resource.Dispose();
            }
        }

        #endregion
    }
}
