namespace Asv.IO;

public sealed class Int32Type : IntegerType
{
    public static readonly Int32Type Default = new();

    public override FieldTypeId TypeId => FieldTypeId.Int32;
    public override string Name => "int32";
    public override int BitWidth => 32;
    public override int ByteWidth => 4;
    public override bool IsSigned => true;

    public static void Accept(IVisitor visitor, Field field, ref int value) => FieldType.Accept(visitor, field, ref value);
    
}

