namespace Asv.Modeling;

public interface ISupportParent<out TBase>
    where TBase : ISupportParent<TBase>
{
    TBase? Parent { get; }
}