namespace Asv.Modeling;

public interface ISupportRoutedEvents<TBase> : ISupportParent<TBase>, ISupportChildren<TBase>
    where TBase : ISupportRoutedEvents<TBase>
{
    IRoutedEventController<TBase> Events { get; }
}
