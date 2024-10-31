using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZLogger;

namespace Asv.Cfg
{
    public class InMemoryConfiguration(ILogger? logger = null) : IConfiguration
    {
        private readonly Dictionary<string, JToken> _values = new();
        private readonly ReaderWriterLockSlim _rw = new(LockRecursionPolicy.SupportsRecursion);
        private readonly ILogger _logger = logger ?? NullLogger.Instance;

        public void Dispose()
        {
            _logger.ZLogTrace($"Dispose {nameof(InMemoryConfiguration)}");
            _rw.Dispose();
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

        public TPocoType Get<TPocoType>(string key, Lazy<TPocoType> defaultValue)
        {
            try
            {
                _rw.EnterUpgradeableReadLock();
                if (_values.TryGetValue(key, out var value))
                {
                    return value.ToObject<TPocoType>() ?? defaultValue.Value;
                }
                else
                {
                    Set(key, defaultValue.Value);
                    return defaultValue.Value;
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
                var jValue = JsonConvert.DeserializeObject<JToken>(
                    JsonConvert.SerializeObject(value)
                );
                Debug.Assert(jValue != null, nameof(jValue) + " != null");
                _logger.ZLogTrace($"Set configuration key [{key}]");
                _values[key] = jValue;
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
                _values.Remove(key);
                _logger.ZLogTrace($"Remove configuration key [{key}]");
            }
            finally
            {
                _rw.ExitWriteLock();
            }
        }
    }
}
