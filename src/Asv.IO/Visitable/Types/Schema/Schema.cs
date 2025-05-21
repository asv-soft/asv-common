using System.Collections.Immutable;

namespace Asv.IO;

public partial class Schema(ImmutableArray<Field> fields, ImmutableDictionary<string, string> metadata)  
    : StructType(fields)
{
    public new const string TypeId = "schema";
    public static Schema Empty => new Schema(ImmutableArray<Field>.Empty, ImmutableDictionary<string, string>.Empty);
    
    public override string Name => TypeId;
    public ImmutableDictionary<string, string> Metadata => metadata;
    public Schema RemoveField(int fieldIndex) => new(Fields.RemoveAt(fieldIndex), Metadata);
    public Schema InsertField(int fieldIndex, Field newField) => new(Fields.Add(newField), Metadata);
    public Schema SetField(int fieldIndex, Field newField) => new(Fields.SetItem(fieldIndex,newField), Metadata);

    
}

