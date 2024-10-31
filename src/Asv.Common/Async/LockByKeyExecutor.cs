using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Asv.Common
{
    /// <summary>
    /// This class implement lock by TKey behavior (for example lock by string).
    /// </summary>
    public class LockByKeyExecutor<TKey>
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, object> _lockDictionary;

        public LockByKeyExecutor(IEqualityComparer<TKey> comparer)
        {
            ArgumentNullException.ThrowIfNull(comparer);

            _lockDictionary = new ConcurrentDictionary<TKey, object>(comparer);
        }

        public LockByKeyExecutor()
        {
            _lockDictionary = new ConcurrentDictionary<TKey, object>();
        }

        /// <summary>
        /// Execute action with lock by TKey: if TKey is equal then action will be executed in one thread
        /// </summary>
        /// <param name="lockString">.</param>
        /// <param name="action">.</param>
        public void Execute(TKey lockString, Action action)
        {
            ArgumentNullException.ThrowIfNull(lockString);
            ArgumentNullException.ThrowIfNull(action);

            var thisThreadSyncObject = new object();
            lock (thisThreadSyncObject)
            {
                try
                {
                    for (; ; )
                    {
                        var runningThreadSyncObject = _lockDictionary.GetOrAdd(
                            lockString,
                            thisThreadSyncObject
                        );
                        if (runningThreadSyncObject == thisThreadSyncObject)
                        {
                            break;
                        }

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

        public void Execute<TArg>(TKey lockString, TArg arg, Action<TArg> action)
        {
            ArgumentNullException.ThrowIfNull(lockString);
            ArgumentNullException.ThrowIfNull(action);

            var thisThreadSyncObject = new object();
            lock (thisThreadSyncObject)
            {
                try
                {
                    for (; ; )
                    {
                        var runningThreadSyncObject = _lockDictionary.GetOrAdd(
                            lockString,
                            thisThreadSyncObject
                        );
                        if (runningThreadSyncObject == thisThreadSyncObject)
                        {
                            break;
                        }

                        lock (runningThreadSyncObject)
                        {
                            // Wait for the currently processing thread to finish and try inserting into the dictionary again.
                        }
                    }

                    action(arg);
                }
                finally
                {
                    // Remove the key from the lock dictionary
                    _lockDictionary.TryRemove(lockString, out _);
                }
            }
        }

        public void Execute<TArg1, TArg2>(
            TKey lockString,
            TArg1 arg1,
            TArg2 arg2,
            Action<TArg1, TArg2> action
        )
        {
            ArgumentNullException.ThrowIfNull(lockString);
            ArgumentNullException.ThrowIfNull(action);

            var thisThreadSyncObject = new object();
            lock (thisThreadSyncObject)
            {
                try
                {
                    for (; ; )
                    {
                        var runningThreadSyncObject = _lockDictionary.GetOrAdd(
                            lockString,
                            thisThreadSyncObject
                        );
                        if (runningThreadSyncObject == thisThreadSyncObject)
                        {
                            break;
                        }

                        lock (runningThreadSyncObject)
                        {
                            // Wait for the currently processing thread to finish and try inserting into the dictionary again.
                        }
                    }

                    action(arg1, arg2);
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
            ArgumentNullException.ThrowIfNull(lockString);
            ArgumentNullException.ThrowIfNull(action);

            var thisThreadSyncObject = new object();
            lock (thisThreadSyncObject)
            {
                try
                {
                    for (; ; )
                    {
                        var runningThreadSyncObject = _lockDictionary.GetOrAdd(
                            lockString,
                            thisThreadSyncObject
                        );
                        if (runningThreadSyncObject == thisThreadSyncObject)
                        {
                            break;
                        }

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

        public TResult Execute<TResult, TArg>(TKey lockString, TArg arg, Func<TArg, TResult> action)
        {
            ArgumentNullException.ThrowIfNull(lockString);
            ArgumentNullException.ThrowIfNull(action);

            var thisThreadSyncObject = new object();
            lock (thisThreadSyncObject)
            {
                try
                {
                    for (; ; )
                    {
                        var runningThreadSyncObject = _lockDictionary.GetOrAdd(
                            lockString,
                            thisThreadSyncObject
                        );
                        if (runningThreadSyncObject == thisThreadSyncObject)
                        {
                            break;
                        }

                        lock (runningThreadSyncObject)
                        {
                            // Wait for the currently processing thread to finish and try inserting into the dictionary again.
                        }
                    }

                    return action(arg);
                }
                finally
                {
                    // Remove the key from the lock dictionary
                    _lockDictionary.TryRemove(lockString, out _);
                }
            }
        }

        public TResult Execute<TResult, TArg1, TArg2>(
            TKey lockString,
            TArg1 arg1,
            TArg2 arg2,
            Func<TArg1, TArg2, TResult> action
        )
        {
            ArgumentNullException.ThrowIfNull(lockString);
            ArgumentNullException.ThrowIfNull(action);

            var thisThreadSyncObject = new object();
            lock (thisThreadSyncObject)
            {
                try
                {
                    for (; ; )
                    {
                        var runningThreadSyncObject = _lockDictionary.GetOrAdd(
                            lockString,
                            thisThreadSyncObject
                        );
                        if (runningThreadSyncObject == thisThreadSyncObject)
                        {
                            break;
                        }

                        lock (runningThreadSyncObject)
                        {
                            // Wait for the currently processing thread to finish and try inserting into the dictionary again.
                        }
                    }

                    return action(arg1, arg2);
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
