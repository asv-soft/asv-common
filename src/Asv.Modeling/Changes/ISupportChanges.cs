using R3;

namespace Asv.Modeling;

public interface ISupportChanges
{
    ReadOnlyReactiveProperty<bool> HasChanges { get; }
}
