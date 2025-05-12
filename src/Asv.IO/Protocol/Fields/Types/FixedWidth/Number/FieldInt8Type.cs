namespace Asv.IO;

public sealed class FieldInt8Type : FieldIntegerType
{
    public const string TypeName = "int8";
    
    public static readonly FieldInt8Type Default = new();

    public override FieldTypeId TypeId => FieldTypeId.Int8;
    public override string Name => TypeName;
    public override int BitWidth => 8;
    public override bool IsSigned => true;

    public override void Accept(IFieldTypeVisitor visitor) => Accept(this, visitor);
}