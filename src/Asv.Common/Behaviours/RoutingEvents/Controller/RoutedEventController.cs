using System;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.Common;

public class RoutedEventController<T>(T owner) : AsyncDisposableOnceBag, IRoutedEventController<T>
    where T : ISupportRoutedEvents<T>
{
    private RoutedEventHandler<T>? _routedEventHandler;
    public T Owner => owner;

    public async ValueTask Rise(AsyncRoutedEvent<T> routedEvent, CancellationToken cancel = default)
    {
        if (IsDisposed)
        {
            return;
        }
        if (_routedEventHandler != null)
        {
            await _routedEventHandler.Invoke(Owner, routedEvent);
            if (routedEvent.IsHandled)
            {
                return;
            }
        }

        switch (routedEvent.Strategy)
        {
            case RoutingStrategy.Bubble:
                if (Owner.Parent is not null)
                {
                    await Owner.Parent.Events.Rise(routedEvent, cancel);
                }
                break;
            case RoutingStrategy.Tunnel:
                foreach (var child in Owner.GetChildren())
                {
                    await child.Events.Rise(routedEvent, cancel);
                    if (routedEvent.IsHandled)
                    {
                        return;
                    }
                }
                break;
            case RoutingStrategy.Direct:
                // do nothing
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public IDisposable Subscribe(RoutedEventHandler<T> handler)
    {
        _routedEventHandler += handler;
        return R3.Disposable.Create(handler, RemoveHandler);
    }

    public void RemoveHandler(RoutedEventHandler<T> handler)
    {
        if (_routedEventHandler == null)
        {
            return;
        }
        _routedEventHandler -= handler;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _routedEventHandler = null;
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        _routedEventHandler = null;
        await base.DisposeAsyncCore();
    }
}
