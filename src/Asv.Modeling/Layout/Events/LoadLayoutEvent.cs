namespace Asv.Modeling;

/// <summary>
/// Base routed event used to load a layout value for a sender.
/// </summary>
/// <typeparam name="TBase">The routed event base type.</typeparam>
/// <param name="sender">The object requesting layout loading.</param>
/// <param name="layoutId">The identifier of the layout value to load.</param>
public abstract class LoadLayoutEvent<TBase>(TBase sender, string layoutId)
    : AsyncRoutedEvent<TBase>(sender, RoutingStrategy.Bubble)
    where TBase : ISupportRoutedEvents<TBase>
{
    /// <summary>
    /// Gets the identifier of the layout value to load.
    /// </summary>
    public string LayoutId => layoutId;

    /// <summary>
    /// Gets the loaded layout value as an untyped object.
    /// </summary>
    /// <exception cref="InvalidOperationException">The layout value has not been loaded.</exception>
    public abstract object UntypedLayoutData { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the layout value was loaded.
    /// </summary>
    public bool IsLoaded { get; set; }

    internal abstract bool TryLoad(ILayoutStore store, NavPath path);
}

/// <summary>
/// Routed event used to load a typed layout value for a sender.
/// </summary>
/// <typeparam name="TBase">The routed event base type.</typeparam>
/// <typeparam name="TData">The layout value type.</typeparam>
/// <param name="sender">The object requesting layout loading.</param>
/// <param name="layoutId">The identifier of the layout value to load.</param>
public sealed class LoadLayoutEvent<TBase, TData>(TBase sender, string layoutId)
    : LoadLayoutEvent<TBase>(sender, layoutId)
    where TBase : ISupportRoutedEvents<TBase>
{
    private TData _layoutData = default!;

    /// <summary>
    /// Gets the loaded layout value.
    /// </summary>
    /// <exception cref="InvalidOperationException">The layout value has not been loaded.</exception>
    public TData LayoutData =>
        IsLoaded
            ? _layoutData
            : throw new InvalidOperationException("Layout data has not been loaded yet.");

    /// <inheritdoc />
    public override object UntypedLayoutData => LayoutData!;

    internal override bool TryLoad(ILayoutStore store, NavPath path)
    {
        if (store.TryLoad<TData>(path, LayoutId, out var layoutData) == false)
        {
            return false;
        }

        _layoutData = layoutData;
        IsLoaded = true;
        return true;
    }
}
