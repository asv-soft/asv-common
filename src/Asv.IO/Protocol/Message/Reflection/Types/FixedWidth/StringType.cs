namespace Asv.IO;

public sealed class StringType : FieldType
{
    public static readonly StringType Default = new();

    public override FieldTypeId TypeId => FieldTypeId.String;
    public override string Name => "utf8";

    public override void Accept(IFieldTypeVisitor visitor) => Accept(this, visitor);
}