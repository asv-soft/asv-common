using Asv.Common;
using R3;

namespace Asv.Modeling;

public class UndoController<TBase>(TBase owner) : AsyncDisposableOnce, IUndoController
    where TBase : ISupportRoutedEvents<TBase>
{
    private readonly Dictionary<string, IUndoHandler> _registration = new();
    public bool MuteChanges { get; set; } = true;

    public IDisposable Register(IUndoHandler handler)
    {
        if (!_registration.TryAdd(handler.ChangeId, handler))
        {
            throw new UndoExceptionException(
                $"Change handler with id '{handler.ChangeId}' already registered"
            );
        }
        return Disposable.Combine(
            handler
                .Changes.Where(_ => !MuteChanges)
                .SubscribeAwait(
                    handler.ChangeId,
                    (change, id, cancel) => RiseChangeEvent(id, change, cancel)
                ),
            Disposable.Create(handler.ChangeId, x => _registration.Remove(x))
        );
    }

    public IUndoHandler Find(string changeId)
    {
        return _registration[changeId];
    }

    private ValueTask RiseChangeEvent(string id, IChange change, CancellationToken cancel) =>
        owner.Rise(new UndoEvent<TBase>(owner, change, id), cancel);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            MuteChanges = true;
            _registration.Clear();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        MuteChanges = true;
        await base.DisposeAsyncCore();
        _registration.Clear();
    }
}
