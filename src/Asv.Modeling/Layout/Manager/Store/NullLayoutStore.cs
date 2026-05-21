namespace Asv.Modeling;

/// <summary>
/// A layout store implementation that ignores saved values and never loads data.
/// </summary>
public sealed class NullLayoutStore : ILayoutStore
{
    /// <summary>
    /// Gets the shared no-op layout store instance.
    /// </summary>
    public static ILayoutStore Instance { get; } = new NullLayoutStore();

    private NullLayoutStore() { }

    /// <inheritdoc />
    public bool TryLoad<TData>(NavPath path, string layoutId, out TData layoutData)
    {
        layoutData = default!;
        return false;
    }

    /// <inheritdoc />
    public void Save<TData>(NavPath path, string layoutId, TData layoutData) { }

    /// <inheritdoc />
    public void Flush() { }

    /// <inheritdoc />
    public void Dispose() { }
}
