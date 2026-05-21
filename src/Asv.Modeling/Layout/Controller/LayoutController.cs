using Asv.Common;

namespace Asv.Modeling;

/// <summary>
/// Default implementation of <see cref="ILayoutController"/> for an object that supports routed events.
/// </summary>
/// <typeparam name="TBase">The routed event base type used by the owner.</typeparam>
public sealed class LayoutController<TBase> : AsyncDisposableOnce, ILayoutController
    where TBase : ISupportRoutedEvents<TBase>
{
    private readonly TBase _owner;
    private readonly Dictionary<string, LayoutRegistration> _registration = new(4);

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutController{TBase}"/> class.
    /// </summary>
    /// <param name="owner">The object that owns the registered layout values.</param>
    public LayoutController(TBase owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _owner = owner;
    }

    /// <inheritdoc />
    public ILayoutSink<TData> Register<TData>(string layoutId, LoadLayoutCallback<TData> loadLayout)
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

    /// <inheritdoc />
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
