using R3;

namespace Asv.Modeling.Changes;

public interface ISupportChanges
{
    ReadOnlyReactiveProperty<bool> HasChanges { get; }
}
