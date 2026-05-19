namespace Asv.Modeling;

public interface ISupportLayout
{
    ILayoutController Layout { get; }
}

public interface ISupportLayout<TBase>
    : ISupportLayout,
        ISupportRoutedEvents<TBase>,
        ISupportNavigation<TBase>
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>;
