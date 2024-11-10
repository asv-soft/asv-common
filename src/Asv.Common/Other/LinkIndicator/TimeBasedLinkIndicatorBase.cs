using System;
using System.Threading;

namespace Asv.Common;

public class TimeBasedLinkIndicatorBase : LinkIndicatorBase
{
    private readonly TimeSpan _timeout;
    private readonly TimeProvider _timeProvider;
    private readonly ITimer _timer;
    private long _lastTime;

    public TimeBasedLinkIndicatorBase(TimeSpan timeout,int downgradeErrors = 3,TimeProvider? timeProvider = null) : base(downgradeErrors)
    {
        _timeout = timeout;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _lastTime = _timeProvider.GetTimestamp();
        _timer = _timeProvider.CreateTimer(CheckTimeout,null, timeout, timeout);
            
    }

    private void CheckTimeout(object? state)
    {
        if (_timeProvider.GetElapsedTime(Interlocked.Read(ref _lastTime)) > _timeout)
        {
            InternalDowngrade();
        }
    }

    protected override void InternalUpgrade()
    {
        base.InternalUpgrade();
        Interlocked.Exchange(ref _lastTime, _timeProvider.GetTimestamp());
    }

}