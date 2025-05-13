namespace Asv.IO;

public sealed class FloatType: FloatingPointType
{
    public static readonly FloatType Default = new();

    public override FieldTypeId TypeId => FieldTypeId.Float;
    public override string Name => "float";
    public override int BitWidth => 32;
    public override int ByteWidth => 4;
    public override bool IsSigned => true;
    public override PrecisionKind Precision => PrecisionKind.Single;

    public override void Accept(IFieldTypeVisitor visitor) => Accept(this, visitor);
}