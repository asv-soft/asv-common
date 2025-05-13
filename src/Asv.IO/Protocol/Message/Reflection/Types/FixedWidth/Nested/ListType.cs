using System.Collections.Immutable;

namespace Asv.IO;

public sealed class ListType(Field valueField) : NestedType([valueField])
{
    public ListType(FieldType valueDataType)
        : this(new Field("item", valueDataType, ImmutableDictionary<string, string>.Empty)) { }
    public override FieldTypeId TypeId => FieldTypeId.List;
    public override string Name => "list";

    public Field ValueField => Fields[0];

    public IFieldType ValueDataType => Fields[0].DataType;

    public override void Accept(IFieldTypeVisitor visitor) => Accept(this, visitor);
}