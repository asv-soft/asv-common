using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Asv.Common;

public class UndoController<TBase>(TBase owner) : AsyncDisposableOnce, IUndoController
    where TBase : ISupportRoutedEvents<TBase>
{
    private readonly Dictionary<string, IUndoHandler> _registration = new();
    public bool MuteUndoChanges { get; set; } = false;

    public IDisposable Register(IUndoHandler handler)
    {
        if (!_registration.TryAdd(handler.RegistrationId, handler))
        {
            throw new UndoExceptionException(
                $"Change handler with id '{handler.RegistrationId}' already registered"
            );
        }
        return Disposable.Combine(
            handler
                .Changes.Where(_ => !MuteUndoChanges)
                .SubscribeAwait(handler.RegistrationId, RiseChangeEvent),
            Disposable.Create(handler.RegistrationId, x => _registration.Remove(x))
        );
    }

    public IUndoHandler Find(string changeId)
    {
        return _registration[changeId];
    }

    private ValueTask RiseChangeEvent(IChange change, string id, CancellationToken cancel) =>
        owner.Rise(new UndoEvent<TBase>(owner, change, id), cancel);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            MuteUndoChanges = true;
            _registration.Clear();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        MuteUndoChanges = true;
        await base.DisposeAsyncCore();
        _registration.Clear();
    }
}
