namespace Asv.IO;

public sealed class UInt16Type : IntegerType
{
    public static readonly UInt16Type Default = new();

    public override FieldTypeId TypeId => FieldTypeId.UInt16;
    public override string Name => "uint16";
    public override int BitWidth => 16;
    public override int ByteWidth => 2;
    public override bool IsSigned => false;

    public static void Accept(IVisitor visitor, Field field, ref ushort value) => FieldType.Accept(visitor, field, ref value);
   
}