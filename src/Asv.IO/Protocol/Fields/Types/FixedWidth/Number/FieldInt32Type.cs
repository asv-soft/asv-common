namespace Asv.IO;

public sealed class FieldInt32Type : FieldIntegerType
{
    public const string TypeName = "int32";
    
    public static readonly FieldInt32Type Default = new();

    public override FieldTypeId TypeId => FieldTypeId.Int32;
    public override string Name => TypeName;
    public override int BitWidth => 8*sizeof(int);
    public override bool IsSigned => true;

    public override void Accept(IFieldTypeVisitor visitor) => Accept(this, visitor);
}