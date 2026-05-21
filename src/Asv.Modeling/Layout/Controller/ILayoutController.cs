namespace Asv.Modeling;

/// <summary>
/// Represents a registered layout entry that can load and save one layout value.
/// </summary>
/// <typeparam name="TData">The value type stored for the layout entry.</typeparam>
public interface ILayoutSink<in TData> : IDisposable
{
    /// <summary>
    /// Starts loading the registered layout value in the background.
    /// </summary>
    void Load();

    /// <summary>
    /// Loads the registered layout value and applies it through the registration callback.
    /// </summary>
    /// <param name="cancel">The cancellation token for the load operation.</param>
    /// <returns>A task that completes when the layout value is loaded and applied.</returns>
    ValueTask LoadAsync(CancellationToken cancel = default);

    /// <summary>
    /// Saves the specified layout value for this registration.
    /// </summary>
    /// <param name="data">The layout value to save.</param>
    void Save(TData data);
}

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
    /// Starts loading all registered layout values.
    /// </summary>
    void LoadAll();
}
