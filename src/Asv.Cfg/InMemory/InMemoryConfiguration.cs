using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using R3;
using ZLogger;

namespace Asv.Cfg
{
    public class InMemoryConfiguration(ILogger? logger = null) : ConfigurationBase
    {
        

        private readonly Dictionary<string, JToken> _values = new();
        private readonly ReaderWriterLockSlim _rw = new(LockRecursionPolicy.SupportsRecursion);
        private readonly ILogger _logger = logger ?? NullLogger.Instance;
        private readonly Subject<ConfigurationException> _onError = new();

        protected override IEnumerable<string> InternalSafeGetReservedParts() => Array.Empty<string>();

        protected override IEnumerable<string> InternalSafeGetAvailableParts()
        {
            try
            {
                _rw.EnterReadLock();
                return _values.Keys.ToImmutableArray();
            }
            finally
            {
                _rw.ExitReadLock();
            }
        }

        protected override bool InternalSafeExist(string key)
        {
            try
            {
                _rw.EnterReadLock();
                return _values.ContainsKey(key);
            }
            finally
            {
                _rw.ExitReadLock();
            }
            
        }

        protected override TPocoType InternalSafeGet<TPocoType>(string key, Lazy<TPocoType> defaultValue)
        {
            _rw.EnterUpgradeableReadLock();
            try
            {
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


        protected override void InternalSafeSave<TPocoType>(string key, TPocoType value)
        {
            try
            {
                _rw.EnterWriteLock();
                var jValue = JsonConvert.DeserializeObject<JToken>(JsonConvert.SerializeObject(value));
                Debug.Assert(jValue != null, nameof(jValue) + " != null");
                _logger.ZLogTrace($"Set configuration key [{key}]");
                _values[key] = jValue;
            }
            finally
            {
                _rw.ExitWriteLock();
            }
        }

        protected override void InternalSafeRemove(string key)
        {
            try
            {
                _rw.EnterWriteLock();
                _values.Remove(key);
                
            }
            finally
            {
                _rw.ExitWriteLock();
            }
        }
        
        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _rw.Dispose();
                _onError.Dispose();
                _values.Clear();
            }

            base.Dispose(disposing);
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            await CastAndDispose(_rw);
            await CastAndDispose(_onError);
            await base.DisposeAsyncCore();
            _values.Clear();
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
