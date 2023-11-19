using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Asv.Cfg
{
    public class InMemoryConfiguration:IConfiguration
    {
        private readonly Dictionary<string, JToken> _values = new();
        private readonly ReaderWriterLockSlim _rw = new(LockRecursionPolicy.SupportsRecursion);

        public void Dispose()
        {
            _rw?.Dispose();
            _values.Clear();
        }

        public IEnumerable<string> AvailableParts => GetParts();

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
                    Set(key, defaultValue);
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
                    _values[key] = jValue;
                }
                else
                {
                    _values.Add(key, jValue);
                }
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
                    _values.Remove(key);
                }
            }
            finally
            {
                _rw.ExitWriteLock();
            }
        }

        public IObservable<Exception> OnError { get; }
    }
}
