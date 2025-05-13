namespace Asv.IO;

public sealed class Int16Type : FieldIntegerType
{
    public static readonly Int16Type Default = new();

    public override FieldTypeId TypeId => FieldTypeId.Int16;
    public override string Name => "int16";
    public override int BitWidth => 16;
    public override int ByteWidth => 2;
    public override bool IsSigned => true;

    public override void Accept(IFieldTypeVisitor visitor) => Accept(this, visitor);
}