namespace Asv.Modeling;

/// <summary>
/// Represents a registered layout entry that can load and save one layout value.
/// </summary>
/// <typeparam name="TData">The value type stored for the layout entry.</typeparam>
public interface ILayoutSink<in TData> : IDisposable
{
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
    /// <param name="cancel">The cancellation token for the save operation.</param>
    /// <returns>A task that completes when the layout value is saved.</returns>
    ValueTask SaveAsync(TData data, CancellationToken cancel = default);
}