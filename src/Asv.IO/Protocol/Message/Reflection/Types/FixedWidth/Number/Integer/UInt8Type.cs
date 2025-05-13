namespace Asv.IO;

public sealed class UInt8Type : FieldIntegerType
{
    public static readonly UInt8Type Default = new();

    public override FieldTypeId TypeId => FieldTypeId.UInt8;
    public override string Name => "uint8";
    public override int BitWidth => 8;
    public override int ByteWidth => 1;
    public override bool IsSigned => false;

    public override void Accept(IFieldTypeVisitor visitor) => Accept(this, visitor);
}