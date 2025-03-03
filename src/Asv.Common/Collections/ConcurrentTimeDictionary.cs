using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Asv.Common
{
    public class ConcurrentTimeDictionary<TKey,TValue> : IDisposable
        where TValue:class where TKey : notnull
    {
        private readonly TimeSpan _maxAge;
        private readonly Func<TValue, DateTime> _getTimeCallback;
        private readonly ITimeService _timeService;
        private readonly Dictionary<TKey,TValue> _dict = new();
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

        public ConcurrentTimeDictionary(TimeSpan maxAge, Func<TValue, DateTime> getTimeCallback, ITimeService timeService)
        {
            _timeService = timeService;
            _getTimeCallback = getTimeCallback;
            _maxAge = maxAge;
        }

        public TKey[] GetKeys()
        {
            _lock.EnterReadLock();
            var result = _dict.Keys.ToArray();
            _lock.ExitReadLock();
            return result;
        }

        public void AddOrUpdate(TKey key,Func<TValue> create,Action<TValue> update)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_dict.TryGetValue(key,out var value))
                {
                    update(value);
                }
                else
                {
                    _dict.Add(key,create());
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public T GetValues<T>(TKey key, Func<TValue, T> getValues,Func<T> notFoundCallback)
        {
            T result;
            _lock.EnterReadLock();
            if (_dict.TryGetValue(key, out var value))
            {
                result = getValues(value);
            }
            else
            {
                result = notFoundCallback();
            }
            _lock.ExitReadLock();
            return result;
        }

        public void ClearOld()
        {
            _lock.EnterUpgradeableReadLock();
            var now = _timeService.Now;
            var itemsToDelete = new List<TKey>();
            foreach (var value in _dict)
            {
                var valueTime = _getTimeCallback(value.Value);
                if (now - valueTime < TimeSpan.Zero || now - valueTime > _maxAge)
                {
                    itemsToDelete.Add(value.Key);
                }
            }

            if (itemsToDelete.Count != 0)
            {
                _lock.EnterWriteLock();
                foreach (var key in itemsToDelete)
                {
                    _dict.Remove(key);
                }
                _lock.ExitWriteLock();
            }
            _lock.ExitUpgradeableReadLock();
        }

        public void Dispose()
        {
            _lock.EnterWriteLock();
            _dict.Clear();
            _lock.ExitWriteLock();
            _lock.Dispose();
        }
    }
}
