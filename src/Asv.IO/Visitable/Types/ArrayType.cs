using System;
using System.Collections.Immutable;

namespace Asv.IO;

public sealed class ArrayType(Field valueField, int size) : NestedType([valueField])
{
    public ArrayType(FieldType valueDataType, int size)
        : this(new Field("item", valueDataType, ImmutableDictionary<string, string>.Empty), size) { }
    public override FieldTypeId TypeId => FieldTypeId.Array;
    public override string Name => "array";
    public int Size { get; } = size;
    public Field ValueField => Fields[0];
    public IFieldType ValueDataType => Fields[0].DataType;
    
    public static void Accept(IVisitor visitor, Field field, int size, Action<int,IVisitor> callback)
    {
        if (visitor is IArrayVisitor accept)
        {
            accept.BeginArray(field,size);
            for (var i = 0; i < size; i++)
            {
                callback(i, visitor);
            }
            accept.EndArray();
        }
        else
        {
            visitor.VisitUnknown(field);
        }
    }
}


