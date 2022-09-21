using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NLog;

namespace Asv.Cfg.Json
{
    
    public class JsonOneFileConfiguration : IConfiguration
    {
        private readonly string _fileName;
        private readonly Dictionary<string, JToken> _values = new();
        private readonly ReaderWriterLockSlim _rw = new(LockRecursionPolicy.SupportsRecursion);
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly Subject<Unit> _onNeedToSave = new();
        private readonly IDisposable _saveSubscribe;


        public JsonOneFileConfiguration(string fileName, bool createIfNotExist, TimeSpan? flushToFileDelayMs)
        {
            _fileName = fileName;

            if (flushToFileDelayMs == null)
            {
                Logger.Debug($"{fileName} create:{createIfNotExist} flush: No, write immediately ");
            }
            else
            {
                Logger.Debug($"{fileName} create:{createIfNotExist} flush: every {flushToFileDelayMs.Value.TotalSeconds:F2} seconds");
            }

            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("Config file name cannot be null or empty.", nameof(fileName));

            var dir = Path.GetDirectoryName(Path.GetFullPath(fileName));
            if (string.IsNullOrWhiteSpace(dir)) throw new InvalidOperationException("Directory path is null");

            _saveSubscribe = flushToFileDelayMs == null
                ? _onNeedToSave.Subscribe(InternalSaveChanges)
                : _onNeedToSave.Throttle(flushToFileDelayMs.Value).Subscribe(InternalSaveChanges);

            if (!Directory.Exists(dir))
            {
                Logger.Warn($"Directory with config file not exist. Try to create it: {dir}");
                Directory.CreateDirectory(dir);
            }
            
            
            if (File.Exists(fileName) == false)
            {
                if (createIfNotExist)
                {
                    Logger.Warn($"Config file not exist. Try to create {fileName}");
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
                    _values = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(text, new StringEnumConverter()) ?? new Dictionary<string, JToken>();
                }
                catch (Exception e)
                {
                    Logger.Error(e,$"Error to load JSON configuration from file. File content: {text}");
                    throw;
                }
            }
        }

        private void InternalSaveChanges(Unit unit)
        {
            try
            {
                var content = JsonConvert.SerializeObject(_values, Formatting.Indented, new StringEnumConverter());
                File.Delete(_fileName);
                File.WriteAllText(_fileName, content);
                Logger.Trace("Flush configuration to file");
            }
            catch (Exception e)
            {
                Logger.Error(e,$"Error to serialize configutation and save it to file ({_fileName}):{e.Message}");
                throw;
            }
        }

        public IEnumerable<string> AvalableParts => GetParts();

        private IEnumerable<string> GetParts()
        {
            try
            {
                _rw.EnterReadLock();
                return _values.Keys.ToArray();
            }
            finally
            {
                _rw.ExitReadLock();
            }
            
        }

        public bool Exist<TPocoType>(string key)
        {
            return _values.ContainsKey(key);
        }

        public TPocoType Get<TPocoType>(string key, TPocoType defaultValue)
        {
            try
            {
                _rw.EnterUpgradeableReadLock();
                JToken value;
                if (_values.TryGetValue(key, out value))
                {
                    var a = value.ToObject<TPocoType>();
                    return a;
                }
                else
                {
                    Set(key,defaultValue);
                    return defaultValue;
                }
            }
            finally
            {
                _rw.ExitUpgradeableReadLock();
            }
        }

        public void Set<TPocoType>(string key, TPocoType value)
        {
            try
            {
                _rw.EnterWriteLock();
                var jValue = JsonConvert.DeserializeObject<JToken>(JsonConvert.SerializeObject(value));
                if (_values.ContainsKey(key))
                {
                    Logger.Trace($"Update config part [{key}]");
                    _values[key] = jValue;
                }
                else
                {
                    Logger.Trace($"Add new config part [{key}]");
                    _values.Add(key,jValue);
                }
                _onNeedToSave.OnNext(Unit.Default);
            }
            finally
            {
                _rw.ExitWriteLock();
            }
        }

        public void Remove(string key)
        {
            try
            {
                _rw.EnterWriteLock();
                if (_values.ContainsKey(key))
                {
                    Logger.Trace($"Remove config part [{key}]");
                    _values.Remove(key);
                    _onNeedToSave.OnNext(Unit.Default);
                }
            }
            finally
            {
                _rw.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            _saveSubscribe.Dispose();
            _rw?.Dispose();
            _onNeedToSave?.Dispose();
        }
    }
}
