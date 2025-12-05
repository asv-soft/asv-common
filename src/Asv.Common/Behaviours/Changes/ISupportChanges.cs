using R3;

namespace Asv.Common.Changes;

public interface ISupportChanges
{
    ReadOnlyReactiveProperty<bool> HasChanges { get; }
}
