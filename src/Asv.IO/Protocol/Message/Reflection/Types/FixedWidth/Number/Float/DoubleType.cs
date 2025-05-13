namespace Asv.IO;

public sealed class DoubleType: FloatingPointType
{
    public static readonly DoubleType Default = new();

    public override FieldTypeId TypeId => FieldTypeId.Double;
    public override string Name => "double";
    public override int BitWidth => 64;
    public override int ByteWidth => 8;
    public override bool IsSigned => true;
    public override PrecisionKind Precision => PrecisionKind.Double;

    public override void Accept(IFieldTypeVisitor visitor) => Accept(this, visitor);
}