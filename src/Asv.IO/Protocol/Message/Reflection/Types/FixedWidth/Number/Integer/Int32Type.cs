namespace Asv.IO;

public sealed class Int32Type : FieldIntegerType
{
    public static readonly Int32Type Default = new();

    public override FieldTypeId TypeId => FieldTypeId.Int32;
    public override string Name => "int32";
    public override int BitWidth => 32;
    public override int ByteWidth => 4;
    public override bool IsSigned => true;

    public override void Accept(IFieldTypeVisitor visitor) => Accept(this, visitor);

    public static void Read(ISerializeVisitor src, in int value)
    {
        if (src is ISerializeVisitor<int> accept)
        {
            accept.Write(in value);
        }
    }
    
    public static void Write(IDeserializeVisitor writer, in int value)
    {
        if (writer is IDeserializeVisitor<int> accept)
        {
            //accept.Read(value);
        }
    }
}

