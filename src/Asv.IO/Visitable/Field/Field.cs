using System.Collections.Immutable;

namespace Asv.IO;

public partial class Field(string name, IFieldType type, ImmutableDictionary<string, string> metadata)
{
    public ImmutableDictionary<string, string> Metadata => metadata;
    public IFieldType DataType => type;
    public string Name => name;

}