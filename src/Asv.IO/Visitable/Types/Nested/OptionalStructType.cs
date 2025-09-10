namespace Asv.IO;

public class OptionalStructType(StructType baseType) : FieldType
{
    public const string TypeId = "struct?";
    public override string Name => TypeId;

    public StructType BaseType => baseType;

    public static void Accept<T>(
        Asv.IO.IVisitor visitor,
        Field field,
        IFieldType type,
        ref T? value
    )
        where T : IVisitable, new()
    {
        if (visitor is IVisitor accept)
        {
            var t = (OptionalStructType)type;
            accept.BeginOptionalStruct(field, t, value is not null, out var createNew);
            if (createNew)
            {
                value = new T();
            }
            value?.Accept(visitor);
            accept.EndOptionalStruct(value is not null);
        }
        else
        {
            visitor.VisitUnknown(field, type);
        }
    }

    public static void Accept<T>(Asv.IO.IVisitor visitor, Field field, ref T? value)
        where T : IVisitable, new()
    {
        Accept(visitor, field, field.DataType, ref value);
    }

    public interface IVisitor : Asv.IO.IVisitor
    {
        void BeginOptionalStruct(
            Field field,
            OptionalStructType type,
            bool isPresent,
            out bool createNew
        );
        void EndOptionalStruct(bool isPresent);
    }
}
