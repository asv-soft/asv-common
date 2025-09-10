using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Asv.Common
{
    public class ConcurrentCircularTimeBuffer<T> : IDisposable
    {
        private readonly TimeSpan _maxAge;
        private readonly Func<T, DateTime> _getTimeCallback;
        private readonly int _maxCount;
        private readonly ITimeService _timeService;
        private readonly LinkedList<T> _items = [];
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

        public ConcurrentCircularTimeBuffer(
            TimeSpan maxAge,
            Func<T, DateTime> getTimeCallback,
            int maxCount = int.MaxValue,
            ITimeService? timeService = default
        )
        {
            _maxAge = maxAge;
            _getTimeCallback = getTimeCallback;
            _maxCount = maxCount;
            _timeService = timeService ?? DefaultTimeService.Default;
        }

        public int Count => _items.Count;

        public object? Tag { get; set; }

        public List<T> GetItemsWithTimeMoreThen(DateTime beginTime)
        {
            _lock.EnterReadLock();
            var lastElement = _items.Last;
            var result = new List<T>();
            if (lastElement != null)
            {
                while (true)
                {
                    var lastTime = _getTimeCallback(lastElement.Value);
                    if (lastTime <= beginTime)
                    {
                        result.Add(lastElement.Value);
                        if (lastElement.Previous == null)
                        {
                            break;
                        }

                        lastElement = lastElement.Previous;
                        continue;
                    }

                    break;
                }
            }

            _lock.ExitReadLock();
            return result;
        }

        public void Push(T item)
        {
            _lock.EnterWriteLock();
            var currentTime = _getTimeCallback(item);
            var lastElement = _items.Last;
            if (lastElement == null)
            {
                _items.AddLast(item);
            }
            else
            {
                while (true)
                {
                    var lastTime = _getTimeCallback(lastElement.Value);
                    if (lastTime < currentTime)
                    {
                        _items.AddAfter(lastElement, item);
                        break;
                    }

                    // если дошли до начала, вставляем вперед как самый старый
                    if (lastElement.Previous == null)
                    {
                        _items.AddFirst(item);
                        break;
                    }

                    lastElement = lastElement.Previous;
                }
            }

            // remove items by max count
            while (_items.Count > _maxCount)
            {
                _items.RemoveFirst();
            }

            _lock.ExitWriteLock();
        }

        public void ClearOld()
        {
            if (_items.Count == 0)
            {
                return;
            }

            var now = _timeService.Now;
            _lock.EnterUpgradeableReadLock();
            var current = _items.First;
            var itemsToDeleteFromFirst = 0;
            while (current != null)
            {
                var rcvTime = _getTimeCallback(current.Value);
                if (
                    rcvTime > now
                    || // Элемент из будущего!!! Вдруг системные часы резко ушли, поэтому заглядываем и проверяем, что элементы из будущего. Их тоже удаляем.
                    now - rcvTime >= _maxAge
                )
                {
                    itemsToDeleteFromFirst++;
                    current = current.Next;
                }
                else
                {
                    // Если первый(самый старый) нормальный, то остальные тоже, так как добавляются в конец очереди (т.е. сортированы по времени)
                    current = null;
                }
            }

            if (itemsToDeleteFromFirst > 0)
            {
                _lock.EnterWriteLock();
                for (var i = 0; i < itemsToDeleteFromFirst; i++)
                {
                    _items.RemoveFirst();
                }

                _lock.ExitWriteLock();
            }

            _lock.ExitUpgradeableReadLock();
        }

        public List<T> ClearOldAndGetRemaining()
        {
            if (_items.Count == 0)
            {
                return [];
            }

            var now = _timeService.Now;
            _lock.EnterUpgradeableReadLock();
            var current = _items.First;
            var itemsToDeleteFromFirst = 0;
            while (current != null)
            {
                var rcvTime = _getTimeCallback(current.Value);
                if (
                    rcvTime > now
                    || // Элемент из будущего!!! Вдруг системные часы резко ушли, поэтому заглядываем и проверяем, что элементы из будущего. Их тоже удаляем.
                    now - rcvTime >= _maxAge
                ) // старый пакет
                {
                    itemsToDeleteFromFirst++;
                    current = current.Next;
                }
                else
                {
                    // если первый(самый старый) нормальный, то остальные тоже, так как добавляются в конец очереди (т.е. сортированы по времени)
                    current = null;
                }
            }

            if (itemsToDeleteFromFirst > 0)
            {
                _lock.EnterWriteLock();
                for (var i = 0; i < itemsToDeleteFromFirst; i++)
                {
                    _items.RemoveFirst();
                }

                _lock.ExitWriteLock();
            }

            var result = _items.ToList();
            _lock.ExitUpgradeableReadLock();
            return result;
        }

        public void Dispose()
        {
            _lock.Dispose();
        }

        public List<T> GetAll()
        {
            _lock.EnterReadLock();
            var result = _items.ToList();
            _lock.ExitReadLock();
            return result;
        }
    }
}
