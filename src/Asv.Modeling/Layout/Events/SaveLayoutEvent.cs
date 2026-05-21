namespace Asv.Modeling;

/// <summary>
/// Base routed event used to save a layout value for a sender.
/// </summary>
/// <typeparam name="TBase">The routed event base type.</typeparam>
/// <param name="sender">The object requesting layout saving.</param>
/// <param name="layoutId">The identifier of the layout value to save.</param>
public abstract class SaveLayoutEvent<TBase>(TBase sender, string layoutId)
    : AsyncRoutedEvent<TBase>(sender, RoutingStrategy.Bubble)
    where TBase : ISupportRoutedEvents<TBase>
{
    /// <summary>
    /// Gets the identifier of the layout value to save.
    /// </summary>
    public string LayoutId => layoutId;

    /// <summary>
    /// Gets the layout value as an untyped object.
    /// </summary>
    public abstract object UntypedLayoutData { get; }

    internal abstract void Save(ILayoutStore store, NavPath path);
}

/// <summary>
/// Routed event used to save a typed layout value for a sender.
/// </summary>
/// <typeparam name="TBase">The routed event base type.</typeparam>
/// <typeparam name="TData">The layout value type.</typeparam>
/// <param name="sender">The object requesting layout saving.</param>
/// <param name="layoutData">The layout value to save.</param>
/// <param name="layoutId">The identifier of the layout value to save.</param>
public sealed class SaveLayoutEvent<TBase, TData>(TBase sender, TData layoutData, string layoutId)
    : SaveLayoutEvent<TBase>(sender, layoutId)
    where TBase : ISupportRoutedEvents<TBase>
{
    /// <summary>
    /// Gets the layout value to save.
    /// </summary>
    public TData LayoutData => layoutData;

    /// <inheritdoc />
    public override object UntypedLayoutData => layoutData!;

    internal override void Save(ILayoutStore store, NavPath path)
    {
        store.Save(path, LayoutId, layoutData);
    }
}
