namespace Asv.IO;

public sealed class UInt32Type : FieldIntegerType
{
    public static readonly UInt32Type Default = new();

    public override FieldTypeId TypeId => FieldTypeId.UInt32;
    public override string Name => "uint32";
    public override int BitWidth => 32;
    public override int ByteWidth => 4;
    public override bool IsSigned => false;

    public override void Accept(IFieldTypeVisitor visitor) => Accept(this, visitor);
}