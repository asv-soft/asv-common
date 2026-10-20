using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;

namespace Asv.IO.Test;

internal sealed class DeviceLifetimeTimeProvider : TimeProvider
{
    private readonly FakeTimeProvider _inner = new();
    public List<Timer> Timers { get; } = [];
    public int ActiveTimers => Timers.Count(timer => !timer.IsDisposed);
    public override long TimestampFrequency => _inner.TimestampFrequency;

    public override long GetTimestamp() => _inner.GetTimestamp();

    public override DateTimeOffset GetUtcNow() => _inner.GetUtcNow();

    public void Advance(TimeSpan value) => _inner.Advance(value);

    public override ITimer CreateTimer(
        TimerCallback callback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period
    )
    {
        var timer = new Timer(_inner.CreateTimer(callback, state, dueTime, period));
        Timers.Add(timer);
        return timer;
    }

    internal sealed class Timer(ITimer inner) : ITimer
    {
        public bool IsDisposed { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period) => inner.Change(dueTime, period);

        public void Dispose()
        {
            IsDisposed = true;
            inner.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            IsDisposed = true;
            await inner.DisposeAsync();
        }
    }
}

internal sealed class DeviceLifetimeConnection : ProtocolConnection
{
    public DeviceLifetimeConnection()
        : base("lifetime-test", (IProtocolContext)Protocol.Create(_ => { })) { }

    public Func<IProtocolMessage, CancellationToken, ValueTask>? SendCallback { get; set; }
    public int SendCount { get; private set; }

    public override ValueTask Send(IProtocolMessage message, CancellationToken cancel = default)
    {
        SendCount++;
        return SendCallback?.Invoke(message, cancel) ?? ValueTask.CompletedTask;
    }

    public void Receive(IProtocolMessage message) => InternalPublishRxMessage(message);
}
