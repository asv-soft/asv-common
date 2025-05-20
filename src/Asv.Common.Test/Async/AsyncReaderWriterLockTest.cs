using System.Threading;
using System.Threading.Tasks;
using DotNext.Threading;
using JetBrains.Annotations;
using Xunit;

namespace Asv.Common.Test;

[TestSubject(typeof(AsyncReaderWriterLock))]
public class AsyncReaderWriterLockTest
{

     [Fact]
        public async void WriterLock_ShouldBlockOtherWriters()
        {
            var locker = new AsyncReaderWriterLock();

            await locker.EnterWriteLockAsync();

            var isLocked = false;

            Task.Run(async () =>
            {
                try
                {
                    await locker.EnterWriteLockAsync();
                    isLocked = true;
                }
                catch { }
            });

            // Ensure the lock is still held
            Assert.False(isLocked);

            locker.Release();

            // Now the other writer lock should succeed
            Task.Delay(50).Wait();
            Assert.True(isLocked);
        }

        [Fact]
        public async Task ReaderLock_ShouldAllowMultipleReaders()
        {
            var locker = new AsyncReaderWriterLock();

            await locker.EnterReadLockAsync();

            var reader1Acquired = false;
            var reader2Acquired = false;

            // Start two readers
            var task1 = Task.Run(() =>
            {
                locker.EnterReadLockAsync();
                reader1Acquired = true;
                locker.Release();
            });

            var task2 = Task.Run(() =>
            {
                locker.EnterReadLockAsync();
                reader2Acquired = true;
                locker.Release();
            });

            // Wait for both readers to complete
            await Task.WhenAll(task1, task2);

            Assert.True(reader1Acquired);
            Assert.True(reader2Acquired);

            locker.Release();
        }

        [Fact]
        public async Task WriterLock_ShouldBlockReaders()
        {
            var locker = new AsyncReaderWriterLock();

            await locker.EnterWriteLockAsync();

            var readerBlocked = true;

            var readerTask = Task.Run(async () =>
            {
                await locker.EnterReadLockAsync(CancellationToken.None);
                readerBlocked = false;
                locker.Release();
            });

            // Ensure the reader is blocked
            await Task.Delay(50);
            Assert.True(readerBlocked);

            locker.Release();

            // Wait for the reader to acquire the lock
            await readerTask;
            Assert.False(readerBlocked);
        }

        [Fact]
        public async Task ReaderToWriterUpgrade_ShouldBlock()
        {
            var locker = new AsyncReaderWriterLock();

            await locker.EnterReadLockAsync();

            var writerBlocked = true;

            var writerTask = Task.Run(async () =>
            {
                await locker.EnterWriteLockAsync();
                writerBlocked = false;
                locker.Release();
            });

            // Ensure the writer is blocked
            await Task.Delay(50);
            Assert.True(writerBlocked);

            locker.Release();

            // Wait for the writer to acquire the lock
            await writerTask;
            Assert.False(writerBlocked);
        }

        [Fact]
        public async Task Cancellation_ShouldReleaseLocks()
        {
            var locker = new AsyncReaderWriterLock();
            var cts = new CancellationTokenSource();

            await cts.CancelAsync();

            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await locker.EnterReadLockAsync(cts.Token);
            });

            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await locker.EnterWriteLockAsync(cts.Token);
            });
        }

}