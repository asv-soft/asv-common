namespace Asv.Modeling;

/// <summary>
/// Handles a loaded layout value.
/// </summary>
/// <typeparam name="TData">The loaded value type.</typeparam>
/// <param name="data">The loaded layout value.</param>
/// <param name="cancel">The cancellation token for the handler.</param>
/// <returns>A task that completes when the value has been applied.</returns>
public delegate ValueTask LoadLayoutCallback<in TData>(
    TData data,
    CancellationToken cancel = default
);

/// <summary>
/// Registers layout values for an object and coordinates explicit loading.
/// </summary>
public interface ILayoutController : IDisposable
{
    /// <summary>
    /// Registers a layout value by identifier.
    /// </summary>
    /// <typeparam name="TData">The value type stored for this layout identifier.</typeparam>
    /// <param name="layoutId">The identifier of the layout value within the owner.</param>
    /// <param name="loadLayout">The callback invoked when the layout value is loaded.</param>
    /// <returns>A sink used to load, save, and unregister the layout value.</returns>
    ILayoutSink<TData> Register<TData>(string layoutId, LoadLayoutCallback<TData> loadLayout);

    /// <summary>
    /// Loads all registered layout values and applies them through their registration callbacks.
    /// </summary>
    /// <param name="cancel">The cancellation token for the load operation.</param>
    /// <returns>A task that completes when all registered layout values are loaded.</returns>
    ValueTask LoadAllAsync(CancellationToken cancel = default);
}
