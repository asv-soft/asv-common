using System;
using System.IO.Packaging;
using System.Threading;
using Microsoft.Extensions.Logging;
using R3;

namespace Asv.IO;

public sealed class AsvPackageContext(Lock @lock, Package package, ILogger logger) : IDisposable
{
    private readonly Subject<EventArgs> _onEvents = new();
    public Lock Lock => @lock;
    public Package Package => package;
    public ILogger Logger => logger;

    public void Publish(EventArgs eve) => _onEvents.OnNext(eve);

    public Observable<EventArgs> OnEvents => _onEvents;

    public void Dispose()
    {
        _onEvents.Dispose();
    }
}
