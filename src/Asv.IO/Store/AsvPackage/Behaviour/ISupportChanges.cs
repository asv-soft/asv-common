using R3;

namespace Asv.IO;

public interface ISupportChanges
{
    ReadOnlyReactiveProperty<bool> HasChanges { get; }
}
