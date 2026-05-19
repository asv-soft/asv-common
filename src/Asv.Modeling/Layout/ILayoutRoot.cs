namespace Asv.Modeling;

public interface ILayoutRoot
{
    ILayoutStore LayoutStore { get; }
}

public interface ILayoutRoot<TBase>
    : ILayoutRoot,
        ISupportRoutedEvents<TBase>,
        ISupportNavigation<TBase>
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>;
