using Asv.Common;

namespace Asv.Modeling;

public sealed class LayoutController<TBase> : AsyncDisposableOnce, ILayoutController
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
{
    private readonly TBase _owner;
    private readonly Dictionary<string, LayoutRegistration> _registration = new(4);

    public LayoutController(TBase owner)
    {
        _owner = owner;
    }

    public ILayoutRegistration Create<TData>(
        string layoutId,
        AsyncLoadLayoutCallback<TData> load,
        AsyncSaveLayoutCallback<TData> save,
        Func<TData> factory
    )
        where TData : ILayoutData
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutId);
        ArgumentNullException.ThrowIfNull(load);
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(factory);

        if (_registration.ContainsKey(layoutId))
        {
            throw new InvalidOperationException(
                $"Layout handler with id '{layoutId}' already registered"
            );
        }

        var registration = new LayoutRegistration<TBase, TData>(
            _owner,
            layoutId,
            load,
            save,
            factory,
            RemoveRegistration
        );
        _registration.Add(layoutId, registration);
        return registration;
    }

    public ILayoutRegistration this[string layoutId] => _registration[layoutId];

    public async ValueTask LoadAsync(CancellationToken cancel = default)
    {
        foreach (var registration in _registration.Values.ToArray())
        {
            await registration.LoadAsync(cancel).ConfigureAwait(false);
        }
    }

    public async ValueTask SaveAsync(CancellationToken cancel = default)
    {
        foreach (var registration in _registration.Values.ToArray())
        {
            await registration.SaveAsync(cancel).ConfigureAwait(false);
        }
    }

    private void RemoveRegistration(string layoutId)
    {
        if (_registration.Remove(layoutId, out var registration))
        {
            registration.Dispose();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var registration in _registration.Values.ToArray())
            {
                registration.Dispose();
            }
            _registration.Clear();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        foreach (var registration in _registration.Values.ToArray())
        {
            await registration.DisposeAsync().ConfigureAwait(false);
        }
        _registration.Clear();
        await base.DisposeAsyncCore().ConfigureAwait(false);
    }
}
