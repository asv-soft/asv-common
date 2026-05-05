using Asv.Common;
using R3;

namespace Asv.Modeling;

public class UndoController<TBase>(TBase owner) : AsyncDisposableOnce, IUndoController
    where TBase : ISupportRoutedEvents<TBase>
{
    private readonly Dictionary<string, (IUndoHandler Handler, IDisposable Subscription)> _registration = new(4);
    public bool SuppressChanges { get; set; } = true;

    public void Register(IUndoHandler handler)
    {
        var subscription = handler
            .Changes.Where(_ => !SuppressChanges)
            .SubscribeAwait(
                handler.ChangeId,
                (change, id, cancel) => RiseChangeEvent(id, change, cancel)
            );

        if (!_registration.TryAdd(handler.ChangeId, (handler, subscription)))
        {
            subscription.Dispose();
            throw new UndoExceptionException(
                $"Change handler with id '{handler.ChangeId}' already registered"
            );
        }
    }

    public void Unregister(IUndoHandler handler)
    {
        if (_registration.Remove(handler.ChangeId, out var registration))
        {
            registration.Subscription.Dispose();
        }
    }

    public IUndoHandler Find(string changeId)
    {
        return _registration[changeId].Handler;
    }

    private ValueTask RiseChangeEvent(string id, IChange change, CancellationToken cancel) =>
        owner.Rise(new UndoEvent<TBase>(owner, change, id), cancel);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SuppressChanges = true;
            foreach (var registration in _registration.Values)
            {
                registration.Subscription.Dispose();
            }
            _registration.Clear();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        SuppressChanges = true;
        foreach (var registration in _registration.Values)
        {
            registration.Subscription.Dispose();
        }
        await base.DisposeAsyncCore();
        _registration.Clear();
    }
}
