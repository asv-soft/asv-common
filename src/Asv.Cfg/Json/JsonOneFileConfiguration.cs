using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Asv.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NLog;

namespace Asv.Cfg.Json
{
    
    public class JsonOneFileConfiguration : DisposableOnce, IConfiguration
    {
        private readonly string _fileName;
        private readonly bool _sortKeysInFile;
        private readonly ConcurrentDictionary<string, JToken> _values;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly Subject<Unit> _onNeedToSave = new();
        private readonly IDisposable _saveSubscribe;
        private readonly object _sync = new();
        private readonly bool _deferredFlush;
        private readonly Subject<Exception> _deferredErrors = new();


        public JsonOneFileConfiguration(string fileName, bool createIfNotExist, TimeSpan? flushToFileDelayMs, bool sortKeysInFile = false)
        {
            _fileName = fileName;
            _sortKeysInFile = sortKeysInFile;

            if (flushToFileDelayMs == null)
            {
                Logger.Debug($"{fileName} create:{createIfNotExist} flush: No, write immediately ");
                _deferredFlush = false;
            }
            else
            {
                _deferredFlush = true;
                Logger.Debug($"{fileName} create:{createIfNotExist} flush: every {flushToFileDelayMs.Value.TotalSeconds:F2} seconds");
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
                Logger.Warn($"Directory with config file not exist. Try to create it: {dir}");
                Directory.CreateDirectory(dir);
            }
            
            
            if (File.Exists(fileName) == false)
            {
                if (createIfNotExist)
                {
                    Logger.Warn($"Config file not exist. Try to create {fileName}");
                    _values = new ConcurrentDictionary<string, JToken>(ConfigurationHelper.DefaultKeyComparer);
                    InternalSaveChanges(Unit.Default);
                }
                else
                {
                    Logger.Warn($"Config file not exist.");
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
                    Logger.Error(e,$"Error to load JSON configuration from file. File content: {text}");
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
                    var content = _sortKeysInFile ? JsonConvert.SerializeObject(new SortedDictionary<string, JToken>(_values), Formatting.Indented, new StringEnumConverter()) : JsonConvert.SerializeObject(_values, Formatting.Indented, new StringEnumConverter());
                    
                    File.Delete(_fileName);
                    File.WriteAllText(_fileName, content);
                    Logger.Trace("Flush configuration to file");
                }
                catch (Exception e)
                {
                    Logger.Error(e,$"Error to serialize configutation and save it to file ({_fileName}):{e.Message}");
                    if (_deferredFlush == false) throw;
                }
            }
        }

        public IEnumerable<string> AvailableParts => _values.Keys;

        public bool Exist<TPocoType>(string key)
        {
            ConfigurationHelper.ValidateKey(key);
            return _values.ContainsKey(key);
        }

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
