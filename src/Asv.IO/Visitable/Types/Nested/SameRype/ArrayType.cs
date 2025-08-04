using System;

namespace Asv.IO;



public sealed class ArrayType(IFieldType elementType, int size) : FieldType, INestedWithSameType
{
    public const string TypeId = "array";
    public override string Name => TypeId;
    public int Size { get; } = size;
    public IFieldType ElementType => elementType;
    public static void Accept(Asv.IO.IVisitor visitor, Field field, IFieldType type, ElementDelegate callback)  
    {
        if (visitor is IVisitor accept)
        {
            var t = (ArrayType)type;
            accept.BeginArray(field, t);
            for (var i = 0; i < t.Size; i++)
            {
                callback(i, visitor, field, t.ElementType);
            }
            accept.EndArray();
        }
        else
        {
            visitor.VisitUnknown(field, type);
        }
    }

    public static void Accept(Asv.IO.IVisitor visitor, Field field, ElementDelegate callback)
    {
        Accept(visitor, field, field.DataType, callback);
    }
    
    public interface IVisitor: Asv.IO.IVisitor
    {
        void BeginArray(Field field, ArrayType fieldType);
        void EndArray();
    }
}


