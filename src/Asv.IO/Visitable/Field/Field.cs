using System.Collections.Immutable;

namespace Asv.IO;

public partial class Field(string name, IFieldType type, ImmutableDictionary<string, string> metadata) : ISupportTag
{
    public ImmutableDictionary<string, string> Metadata => metadata;
    public IFieldType DataType => type;
    public string Name => name;

    public ref ProtocolTags Tags => throw new System.NotImplementedException();
}