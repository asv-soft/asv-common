using System;
using System.Threading.Tasks;
using R3;

namespace Asv.Common;

public class TimeBasedLinkIndicator(TimeSpan timeout, int downgradeErrors = 3, TimeProvider? timeProvider = null)
    : TimeBasedLinkIndicatorBase(timeout, downgradeErrors, timeProvider)
{
    public void Upgrade()
    {
        InternalUpgrade();
    }
}

public class TimeBasedObservableLinkIndicator<T> : TimeBasedLinkIndicatorBase
{
    private readonly IDisposable _sub1;
    public TimeBasedObservableLinkIndicator(Observable<T> inputStream,
        TimeSpan timeout,
        int downgradeErrors = 3,
        TimeProvider? timeProvider = null) : base(timeout, downgradeErrors, timeProvider)
    {
        _sub1 = inputStream.Subscribe(x=>InternalUpgrade());
    }

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sub1.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_sub1 is IAsyncDisposable sub1AsyncDisposable)
            await sub1AsyncDisposable.DisposeAsync();
        else
            _sub1.Dispose();

        await base.DisposeAsyncCore();
    }

    #endregion

}
