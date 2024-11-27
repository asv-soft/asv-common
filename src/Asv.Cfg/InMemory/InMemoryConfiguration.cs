using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using R3;
using ZLogger;

namespace Asv.Cfg
{
    public class InMemoryConfiguration(ILogger? logger = null) : IConfiguration
    {
        private readonly Dictionary<string, JToken> _values = new();
        private readonly ReaderWriterLockSlim _rw = new(LockRecursionPolicy.SupportsRecursion);
        private readonly ILogger _logger = logger ?? NullLogger.Instance;
        private readonly Subject<ConfigurationException> _onError = new();

        public void Dispose()
        {
            _logger.ZLogTrace($"Dispose {nameof(InMemoryConfiguration)}");
            _rw.Dispose();
            _values.Clear();
            _onError.Dispose();
        }

        public IEnumerable<string> ReservedParts => Array.Empty<string>();
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
            catch (Exception e)
            {
                _logger.ZLogError(e,$"Error to get '{key}' part:{e.Message}");
                var ex = new ConfigurationException($"Error to get '{key}' part",e);
                _onError.OnNext(ex);
                throw ex;
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
                Debug.Assert(jValue != null, nameof(jValue) + " != null");
                _logger.ZLogTrace($"Set configuration key [{key}]");
                _values[key] = jValue;
            }
            catch (Exception e)
            {
                _logger.ZLogError(e,$"Error to set '{key}' part:{e.Message}");
                var ex = new ConfigurationException($"Error to set '{key}' part",e);
                _onError.OnNext(ex);
                throw ex;
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
            catch (Exception e)
            {
                _logger.ZLogError(e,$"Error to remove '{key}' part:{e.Message}");
                var ex = new ConfigurationException($"Error to remove '{key}' part",e);
                _onError.OnNext(ex);
                throw ex;
            }
            finally
            {
                _rw.ExitWriteLock();
            }
        }

        public Observable<ConfigurationException> OnError => _onError;
    }
}
