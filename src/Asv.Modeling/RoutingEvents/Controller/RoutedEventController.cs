using Asv.Common;

namespace Asv.Modeling;

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
            await _routedEventHandler.Invoke(Owner, routedEvent, cancel);
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

    public IDisposable Catch<TEvent>(RoutedEventHandler<T, TEvent> handler) where TEvent : AsyncRoutedEvent<T>
    {
        return Catch(Wrapper);

        async ValueTask Wrapper(T sender, AsyncRoutedEvent<T> e, CancellationToken cancel)
        {
            if (e is TEvent eve)
            {
                await handler(sender, eve, cancel);
            }
        }
    }

    public IDisposable Catch<TEvent>(Action<TEvent> handler)
        where TEvent : AsyncRoutedEvent<T>
    {
        return Catch(Wrapper);

        ValueTask Wrapper(T sender, AsyncRoutedEvent<T> e, CancellationToken cancel)
        {
            if (e is TEvent ev)
            {
                handler(ev);    
            }
            return ValueTask.CompletedTask;
        }
    }

    public IDisposable Catch(RoutedEventHandler<T> handler)
    {
        _routedEventHandler += handler;
        return R3.Disposable.Create(handler, RemoveHandler);
    }

    private void RemoveHandler(RoutedEventHandler<T> handler)
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
