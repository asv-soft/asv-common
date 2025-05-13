using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Asv.IO;

public partial class Schema(ImmutableArray<Field> fieldsList, ImmutableDictionary<string, string> metadata)  
    : NestedType(fieldsList), IRecordType
{
    public static Schema Empty => new Schema(ImmutableArray<Field>.Empty, ImmutableDictionary<string, string>.Empty);
    
    public ImmutableDictionary<string, string> Metadata => metadata;
    public bool HasMetadata => Metadata.IsEmpty;
    

    public Schema RemoveField(int fieldIndex) => new(Fields.RemoveAt(fieldIndex), Metadata);

    public Schema InsertField(int fieldIndex, Field newField) => new(Fields.Add(newField), Metadata);

    public Schema SetField(int fieldIndex, Field newField) => new(Fields.SetItem(fieldIndex,newField), Metadata);

    public override FieldTypeId TypeId => FieldTypeId.Schema;
    public override string Name => nameof(Schema);

    public override void Accept(IFieldTypeVisitor visitor)
    {
        if (visitor is IFieldTypeVisitor<Schema> schemaVisitor)
        {
            schemaVisitor.Visit(this);
        }
        else if (visitor is IFieldTypeVisitor<IRecordType> interfaceVisitor)
        {
            interfaceVisitor.Visit(this);
        }
        else
        {
            visitor.Visit(this);
        }
    }

    public override bool IsFixedWidth => false;
}

