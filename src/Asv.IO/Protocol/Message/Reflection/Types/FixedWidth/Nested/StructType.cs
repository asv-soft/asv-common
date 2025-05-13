using System.Collections.Immutable;

namespace Asv.IO;

public class StructType(ImmutableArray<Field> fields) : NestedType(fields), IRecordType
{
    public override FieldTypeId TypeId => FieldTypeId.Struct;
    public override string Name => "struct";
    public override void Accept(IFieldTypeVisitor visitor)
    {
        if (visitor is IFieldTypeVisitor<StructType> structTypeVisitor)
        {
            structTypeVisitor.Visit(this);
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
}