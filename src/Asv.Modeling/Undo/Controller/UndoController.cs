using Asv.Common;
using R3;

namespace Asv.Modeling;

public sealed class UndoController<TBase> : AsyncDisposableOnce, IUndoController
    where TBase : ISupportRoutedEvents<TBase>
{
    private readonly Subject<(string, IUndoChange)> _changes = new();
    private readonly Dictionary<string, UndoChangeRegistration> _registration = new(4);
    private readonly IDisposable _subscription;
    private int _suppressChangePublicationCount;

    public UndoController(TBase owner)
    {
        _subscription = _changes
            .Where(_ => !IsChangePublicationSuppressed)
            .SubscribeAwait(
                owner,
                (change, sender, cancel) =>
                    sender.Rise(new UndoEvent<TBase>(sender, change.Item2, change.Item1), cancel)
            );
    }

    private bool IsChangePublicationSuppressed =>
        Volatile.Read(ref _suppressChangePublicationCount) > 0;

    public IUndoChangeSink<TChange> Create<TChange>(
        string registrationId,
        UndoCallback<TChange> undo,
        UndoCallback<TChange> redo,
        Func<TChange> factory
    )
        where TChange : IUndoChange
    {
        if (_registration.ContainsKey(registrationId))
        {
            throw new UndoException(
                $"Change handler with id '{registrationId}' already registered"
            );
        }

        var changes = new UndoChangeRegistration<TChange>(
            registrationId,
            undo,
            redo,
            factory,
            _changes,
            RemoveRegistration
        );
        _registration.Add(registrationId, changes);
        return changes;
    }

    private void RemoveRegistration(string registrationId)
    {
        if (_registration.Remove(registrationId, out var registration))
        {
            registration.Dispose();
        }
    }

    public IUndoChangeHandler this[string registrationId] => _registration[registrationId];

    public IDisposable SuppressChangePublication()
    {
        Interlocked.Increment(ref _suppressChangePublicationCount);
        return Disposable.Create(
            this,
            static controller =>
                Interlocked.Decrement(ref controller._suppressChangePublicationCount)
        );
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _subscription.Dispose();
            foreach (var registration in _registration.Values.ToArray())
            {
                registration.Dispose();
            }
            _registration.Clear();
            _changes.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        _subscription.Dispose();
        foreach (var registration in _registration.Values.ToArray())
        {
            await registration.DisposeAsync();
        }
        _registration.Clear();
        _changes.Dispose();
        await base.DisposeAsyncCore();
    }
}
