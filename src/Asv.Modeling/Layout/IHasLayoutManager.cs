namespace Asv.Modeling;

public interface IHasLayoutManager<TBase>
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
{
    ILayoutManager<TBase> LayoutManager { get; }
}
