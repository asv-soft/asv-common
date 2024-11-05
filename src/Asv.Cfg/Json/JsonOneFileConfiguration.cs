using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Asv.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using ZLogger;

namespace Asv.Cfg
{
    
    public class JsonOneFileConfiguration : DisposableOnce, IConfiguration
    {
        private readonly string _fileName;
        private readonly bool _sortKeysInFile;
        private readonly ConcurrentDictionary<string, JToken> _values;
        private readonly Subject<Unit> _onNeedToSave = new();
        private readonly IDisposable _saveSubscribe;
        private readonly object _sync = new();
        private readonly bool _deferredFlush;
        private readonly Subject<Exception> _deferredErrors = new();
        private readonly JsonSerializer _serializer;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;


        public JsonOneFileConfiguration(
            string fileName, 
            bool createIfNotExist, 
            TimeSpan? flushToFileDelayMs, 
            bool sortKeysInFile = false, 
            ILogger? logger = null,
            IFileSystem? fileSystem = null
        )
        {
            _fileSystem= fileSystem ?? new FileSystem();
            _fileName = _fileSystem.Path.GetFullPath(fileName);
            _sortKeysInFile = sortKeysInFile;
            _logger = logger ?? NullLogger.Instance; 
            
            _serializer = JsonHelper.CreateDefaultJsonSerializer();

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
                : _onNeedToSave.Throttle(flushToFileDelayMs.Value).Subscribe(InternalSaveChanges);

            if (_fileSystem.Directory.Exists(dir) == false)
            {
                if (!createIfNotExist)
                    throw new DirectoryNotFoundException($"Directory with config file not exist: {dir}");
                
                _logger.ZLogWarning($"Directory with config file not exist. Try to create it: {dir}");
                _fileSystem.Directory.CreateDirectory(dir);
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
                    _logger.ZLogWarning($"Config file not exist.");
                    throw new Exception($"Configuration file not exist {fileName}");
                }
            }
            else
            {
                var text = _fileSystem.File.ReadAllText(_fileName);
                try
                {
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(text, new StringEnumConverter()) ?? new Dictionary<string, JToken>();
                    _values = new ConcurrentDictionary<string, JToken>(dict, ConfigurationHelper.DefaultKeyComparer);
                }
                catch (Exception e)
                {
                    _logger.ZLogError(e,$"Error to load JSON configuration from file. File content: {text}");
                    throw;
                }
            }
        }
        
        public string FileName => _fileName;
        public IObservable<Exception> OnDeferredError => _deferredErrors;
        private void InternalSaveChanges(Unit unit)
        {
            lock (_sync)
            {
                try
                {
                    if (_fileSystem.File.Exists(_fileName))
                    {
                        _fileSystem.File.Delete(_fileName);
                    }
                    using (var file = _fileSystem.File.CreateText(_fileName))
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
                    _logger.ZLogTrace($"Flush configuration to file [{_values.Count}] ");
                }
                catch (Exception e)
                {
                    _logger.ZLogError(e,$"Error to serialize configuration and save it to file ({_fileName}):{e.Message}");
                    if (_deferredFlush == false) throw;
                }
            }
        }

        public IEnumerable<string> AvailableParts => _values.Keys;

        public bool Exist(string key)
        {
            ConfigurationHelper.ValidateKey(key);
            return _values.ContainsKey(key);
        }

        public TPocoType Get<TPocoType>(string key, Lazy<TPocoType> defaultValue)
        {
            ConfigurationHelper.ValidateKey(key);
            if (_values.TryGetValue(key, out var value))
            {
                return value.ToObject<TPocoType>() ?? throw new InvalidOperationException();
            }

            var inst = defaultValue.Value;
            Set(key,inst);
            return inst;
        }

       
        public void Set<TPocoType>(string key, TPocoType value)
        {
            ConfigurationHelper.ValidateKey(key);

            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            using var jsonTextWriter = new JsonTextWriter(writer);
            jsonTextWriter.Formatting = _serializer.Formatting;
            _serializer.Serialize(jsonTextWriter, value, typeof(TPocoType));
            writer.Flush();          
            stream.Position = 0;
            using var jsonReader = new JsonTextReader(new StreamReader(stream));
            var jValue = _serializer.Deserialize<JToken>(jsonReader) ?? throw new InvalidOperationException();
            _logger.ZLogTrace($"Set configuration key [{key}]");
            _values.AddOrUpdate(key,jValue , (_, _) => jValue);
            _onNeedToSave.OnNext(Unit.Default);
        }

        public void Remove(string key)
        {
            ConfigurationHelper.ValidateKey(key);
            if (_values.TryRemove(key, out _))
            {
                _logger.ZLogTrace($"Remove configuration key [{key}]");
                _onNeedToSave.OnNext(Unit.Default);
            }
        }

        protected override void InternalDisposeOnce()
        {
            _saveSubscribe.Dispose();
            _onNeedToSave.Dispose();
            _logger.ZLogTrace($"Dispose {nameof(JsonOneFileConfiguration)}");
            if (_deferredFlush)
            {
                InternalSaveChanges(Unit.Default);    
            }
            _deferredErrors.Dispose();
            _values.Clear();
        }
    }
}
