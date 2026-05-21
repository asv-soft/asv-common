namespace Asv.Modeling;

/// <summary>
/// Stores layout values by navigation path and layout identifier.
/// </summary>
public interface ILayoutStore : IDisposable
{
    /// <summary>
    /// Attempts to load a layout value.
    /// </summary>
    /// <typeparam name="TData">The expected layout value type.</typeparam>
    /// <param name="path">The navigation path of the object that owns the layout value.</param>
    /// <param name="layoutId">The identifier of the layout value within the owner.</param>
    /// <param name="layoutData">The loaded layout value when the method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> if the layout value was found and deserialized; otherwise, <see langword="false"/>.</returns>
    bool TryLoad<TData>(NavPath path, string layoutId, out TData layoutData);

    /// <summary>
    /// Saves a layout value in memory.
    /// </summary>
    /// <typeparam name="TData">The layout value type.</typeparam>
    /// <param name="path">The navigation path of the object that owns the layout value.</param>
    /// <param name="layoutId">The identifier of the layout value within the owner.</param>
    /// <param name="layoutData">The layout value to save.</param>
    void Save<TData>(NavPath path, string layoutId, TData layoutData);

    /// <summary>
    /// Writes pending layout changes to persistent storage.
    /// </summary>
    void Flush();
}
