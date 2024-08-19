using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Asv.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZLogger;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using ZLogger;

namespace Asv.Cfg.Json
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
        private readonly ILogger<JsonOneFileConfiguration> _logger;


        public JsonOneFileConfiguration(string fileName, bool createIfNotExist, TimeSpan? flushToFileDelayMs, bool sortKeysInFile = false, ILogger<JsonOneFileConfiguration>? logger = null)
        {
            _fileName = fileName;
            _sortKeysInFile = sortKeysInFile;
            _logger = logger ?? NullLogger<JsonOneFileConfiguration>.Instance; 
            
            _serializer = new JsonSerializer
            {
                Formatting = Formatting.Indented,
            };
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

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("Config file name cannot be null or empty.", nameof(fileName));

            var dir = Path.GetDirectoryName(Path.GetFullPath(fileName));
            if (string.IsNullOrWhiteSpace(dir)) throw new InvalidOperationException("Directory path is null");

            _saveSubscribe = flushToFileDelayMs == null
                ? _onNeedToSave.Subscribe(InternalSaveChanges)
                : _onNeedToSave.Throttle(flushToFileDelayMs.Value).Subscribe(InternalSaveChanges);

            if (Directory.Exists(dir) == false && createIfNotExist)
            {
                _logger.ZLogWarning($"Directory with config file not exist. Try to create it: {dir}");
                Directory.CreateDirectory(dir);
            }
            
            
            if (File.Exists(fileName) == false)
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
                var text = File.ReadAllText(_fileName);
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

        public IObservable<Exception> OnDeferredError => _deferredErrors;
        private void InternalSaveChanges(Unit unit)
        {
            lock (_sync)
            {
                try
                {
                    if (File.Exists(_fileName))
                    {
                        File.Delete(_fileName);
                    }
                    using (var file = File.CreateText(_fileName))
                    {
                        //serialize object directly into file stream
                        if (_sortKeysInFile)
                        {
                            _serializer.Serialize(file, _values);    
                        }
                        else
                        {
                            _serializer.Serialize(file, new SortedDictionary<string, JToken>(_values)); 
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

        public TPocoType Get<TPocoType>(string key, TPocoType defaultValue)
        {
            ConfigurationHelper.ValidateKey(key);
            if (_values.TryGetValue(key, out var value))
            {
                var a = value.ToObject<TPocoType>();
                return a;
            }

            Set(key,defaultValue);
            return defaultValue;
        }

        public void Set<TPocoType>(string key, TPocoType value)
        {
            ConfigurationHelper.ValidateKey(key);
            var jValue = JsonConvert.DeserializeObject<JToken>(JsonConvert.SerializeObject(value));
            _values.AddOrUpdate(key,jValue, (s, token) => jValue);
            _onNeedToSave.OnNext(Unit.Default);
        }

        public void Remove(string key)
        {
            ConfigurationHelper.ValidateKey(key);
            if (_values.TryRemove(key, out _))
            {
                _onNeedToSave.OnNext(Unit.Default);
            }
        }

        protected override void InternalDisposeOnce()
        {
            _saveSubscribe.Dispose();
            _onNeedToSave?.Dispose();
            if (_deferredFlush)
            {
                InternalSaveChanges(Unit.Default);    
            }
            _deferredErrors?.Dispose();
            _values.Clear();
        }
    }
}
