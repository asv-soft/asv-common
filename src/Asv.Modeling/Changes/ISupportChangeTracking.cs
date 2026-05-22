using R3;

namespace Asv.Modeling;

public interface ISupportChangeTracking
{
    ReadOnlyReactiveProperty<bool> HasChanges { get; }
}
