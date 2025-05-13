namespace Asv.IO;

public sealed class HalfFloatType: FloatingPointType
{
    public static readonly HalfFloatType Default = new();

    public override FieldTypeId TypeId => FieldTypeId.HalfFloat;
    public override string Name => "halffloat";
    public override int BitWidth => 16;
    public override int ByteWidth => 2;
    public override bool IsSigned => true;
    public override PrecisionKind Precision => PrecisionKind.Half;

    public override void Accept(IFieldTypeVisitor visitor) => Accept(this, visitor);
}