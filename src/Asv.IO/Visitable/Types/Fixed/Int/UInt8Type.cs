namespace Asv.IO;

public sealed class UInt8Type : IntegerType
{
    public static readonly UInt8Type Default = new();

    public override FieldTypeId TypeId => FieldTypeId.UInt8;
    public override string Name => "uint8";
    public override int BitWidth => 8;
    public override int ByteWidth => 1;
    public override bool IsSigned => false;
    
    public static void Accept(IVisitor visitor,Field field, ref byte value) => FieldType.Accept(visitor, field, ref value);
}