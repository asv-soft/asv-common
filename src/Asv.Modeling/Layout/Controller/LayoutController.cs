using Asv.Common;

namespace Asv.Modeling;

public sealed class LayoutController<TBase> : AsyncDisposableOnce, ILayoutController
    where TBase : ISupportRoutedEvents<TBase>
{
    private readonly TBase _owner;
    private readonly Dictionary<string, LayoutRegistration> _registration = new(4);

    public LayoutController(TBase owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _owner = owner;
    }

    public ILayoutSink<TData> Register<TData>(string layoutId, LoadLayoutCallback<TData> loadLayout)
        where TData : ILayoutData
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutId);
        ArgumentNullException.ThrowIfNull(loadLayout);

        if (_registration.ContainsKey(layoutId))
        {
            throw new InvalidOperationException(
                $"Layout handler with id '{layoutId}' already registered"
            );
        }

        var registration = new LayoutRegistration<TBase, TData>(
            _owner,
            layoutId,
            loadLayout,
            RemoveRegistration
        );
        _registration.Add(layoutId, registration);
        return registration;
    }

    public void LoadAll()
    {
        foreach (var registration in _registration.Values.ToArray())
        {
            registration.Load();
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
            registration.Dispose();
        }
        _registration.Clear();
        await base.DisposeAsyncCore().ConfigureAwait(false);
    }
}
