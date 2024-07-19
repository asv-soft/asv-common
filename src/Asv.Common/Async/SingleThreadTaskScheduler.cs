using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.Common
{
    public sealed class SingleThreadTaskScheduler : TaskScheduler, IDisposable
    {
        private const int Disposed = 1;
        private const int NotDisposed = 0;
        private int _disposeFlag;
        private BlockingCollection<Task>? _tasks;
        private readonly Thread _workThread;

        public SingleThreadTaskScheduler([Localizable(false)] string threadName, int boundedCapacity = 256, 
            ThreadPriority priority = ThreadPriority.Normal, ApartmentState apartmentState = ApartmentState.MTA)
        {
            _tasks = new BlockingCollection<Task>(boundedCapacity);
            _workThread = new Thread(() =>
            {
                foreach (var t in _tasks.GetConsumingEnumerable())
                {
                    if (IsDisposed) break;
                    try
                    {
                        TryExecuteTask(t);
                    }
                    catch (Exception ex)
                    {
                        //TODO: at Mono sometimes error (System.InvalidOperationException: The task has already completed)
                        RiseUnhandledTaskException(ex);
                    }
                }
            })
            {
                IsBackground = true,
                Name = threadName,
                Priority = priority
            };
            
            //_workThread.SetApartmentState(apartmentState);
            _workThread.Start();
        }

        protected override void QueueTask(Task task)
        {
            if (_tasks == null) return;
            Debug.Assert(_tasks.Count < _tasks.BoundedCapacity, "Too many tasks");
            if (!_tasks.IsAddingCompleted)
            {
                _tasks.Add(task);
            }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            if (_tasks == null) return ArraySegment<Task>.Empty;
            return _tasks.ToImmutableArray();
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (IsDisposed) return false;
            return
                Thread.CurrentThread.GetApartmentState() == ApartmentState.STA &&
                TryExecuteTask(task);
        }

        public override int MaximumConcurrencyLevel => 1;

        #region Disposing

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposeFlag, Disposed, NotDisposed) != NotDisposed) return;
            if (_tasks == null) return;

            _tasks.CompleteAdding();
            _workThread.Join();

            // Cleanup
            _tasks.Dispose();
            _tasks = null;
        }

        public bool IsDisposed => Thread.VolatileRead(ref _disposeFlag) > 0;

        public void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(IsDisposed,typeof(SingleThreadTaskScheduler));
        }

        #endregion

        #region Thread access

        public Thread SchedulerThread => _workThread;

        public bool CheckAccess()
        {
            return Thread.CurrentThread.ManagedThreadId == _workThread.ManagedThreadId;
        }

        public void VerifyAccess()
        {
            if (!CheckAccess())
            {
                throw new InvalidOperationException("The calling thread does not have access to the data object");
            }

        }

        #endregion

        #region Exceptions

        public event EventHandler<Exception>? OnTaskUnhandledException;

        private void RiseUnhandledTaskException(Exception e)
        {
            OnTaskUnhandledException?.Invoke(this, e);
        }

        #endregion

        public override string ToString()
        {
            return nameof(SingleThreadTaskScheduler);
        }
    }
}
