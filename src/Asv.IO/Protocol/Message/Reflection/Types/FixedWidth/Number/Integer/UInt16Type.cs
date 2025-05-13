namespace Asv.IO;

public sealed class UInt16Type : FieldIntegerType
{
    public static readonly UInt16Type Default = new();

    public override FieldTypeId TypeId => FieldTypeId.UInt16;
    public override string Name => "uint16";
    public override int BitWidth => 16;
    public override int ByteWidth => 2;
    public override bool IsSigned => false;

    public override void Accept(IFieldTypeVisitor visitor) => Accept(this, visitor);
}