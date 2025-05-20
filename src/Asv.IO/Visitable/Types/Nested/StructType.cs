using System.Collections.Immutable;

namespace Asv.IO;

public class StructType(ImmutableArray<Field> fields) : NestedType(fields), IRecordType
{
    public override FieldTypeId TypeId => FieldTypeId.Struct;
    public override string Name => "struct";
    
    public static void Accept(IVisitor visitor, Field field, IVisitable value)
    {
        if (visitor is IStructVisitor accept)
        {
            accept.BeginStruct(field);
            value.Accept(visitor);
            accept.EndStruct();
        }
        else
        {
            visitor.VisitUnknown(field);
        }
    }
}

