using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Asv.Common
{
    /// <summary>
    /// This class implement lock by TKey behavior (for example lock by string)
    /// </summary>
    public class LockByKeyExecutor<TKey> 
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, object> _lockDictionary;

        public LockByKeyExecutor(IEqualityComparer<TKey> comparer)
        {
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));
            _lockDictionary = new ConcurrentDictionary<TKey, object>(comparer);
        }
        public LockByKeyExecutor()
        {
            _lockDictionary = new ConcurrentDictionary<TKey, object>();
        }

        /// <summary>
        /// Execute action with lock by TKey: if TKey is equal then action will be executed in one thread 
        /// </summary>
        /// <param name="lockString"></param>
        /// <param name="action"></param>
        public void Execute(TKey lockString, Action action)
        {
            if (lockString == null) throw new ArgumentNullException(nameof(lockString));
            if (action == null) throw new ArgumentNullException(nameof(action));
            
            var thisThreadSyncObject = new object();
            lock (thisThreadSyncObject)
            {
                try
                {
                    for (; ; )
                    {
                        var runningThreadSyncObject = _lockDictionary.GetOrAdd(lockString, thisThreadSyncObject);
                        if (runningThreadSyncObject == thisThreadSyncObject)
                            break;

                        lock (runningThreadSyncObject)
                        {
                            // Wait for the currently processing thread to finish and try inserting into the dictionary again.
                        }
                    }
                    action();
                }
                finally
                {
                    // Remove the key from the lock dictionary
                    _lockDictionary.TryRemove(lockString, out _);
                }
            }
        }

        public TResult Execute<TResult>(TKey lockString, Func<TResult> action)
        {
            if (lockString == null) throw new ArgumentNullException(nameof(lockString));
            if (action == null) throw new ArgumentNullException(nameof(action));
            
            var thisThreadSyncObject = new object();
            lock (thisThreadSyncObject)
            {
                try
                {
                    for (; ; )
                    {
                        var runningThreadSyncObject = _lockDictionary.GetOrAdd(lockString, thisThreadSyncObject);
                        if (runningThreadSyncObject == thisThreadSyncObject)
                            break;

                        lock (runningThreadSyncObject)
                        {
                            // Wait for the currently processing thread to finish and try inserting into the dictionary again.
                        }
                    }
                    return action();
                }
                finally
                {
                    // Remove the key from the lock dictionary
                    _lockDictionary.TryRemove(lockString, out _);
                }
            }
        }
    }
}