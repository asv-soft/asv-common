namespace Asv.Modeling;

public interface ILayoutManager<TBase> : IDisposable
    where TBase : ISupportRoutedEvents<TBase>, ISupportNavigation<TBase>
{
    ILayoutStore Store { get; }
}
