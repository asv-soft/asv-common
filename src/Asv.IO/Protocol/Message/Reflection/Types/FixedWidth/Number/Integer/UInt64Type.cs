namespace Asv.IO;

public sealed class UInt64Type : FieldIntegerType
{
    public static readonly UInt64Type Default = new();

    public override FieldTypeId TypeId => FieldTypeId.UInt64;
    public override string Name => "uint64";
    public override int BitWidth => 64;
    public override int ByteWidth => 8;
    public override bool IsSigned => false;

    public override void Accept(IFieldTypeVisitor visitor) => Accept(this, visitor);
}