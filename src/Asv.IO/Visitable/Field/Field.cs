using System.Collections.Immutable;

namespace Asv.IO;

public partial class Field(
    string name,
    IFieldType type,
    ImmutableDictionary<string, object?> metadata
)
{
    public ImmutableDictionary<string, object?> Metadata => metadata;
    public IFieldType DataType => type;
    public string Name => name;
}
