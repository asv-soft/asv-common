using R3;

namespace Asv.Modeling;

public interface ISupportParentChange<TBase> : ISupportParent<TBase>
    where TBase : ISupportParent<TBase>
{
    void SetParent(TBase? parent);
    Observable<TBase?> ParentChanged { get; }
}
