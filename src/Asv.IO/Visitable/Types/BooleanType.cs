namespace Asv.IO;

public sealed class BooleanType: FieldNumberType
{
    public static readonly BooleanType Default = new();

    public override FieldTypeId TypeId => FieldTypeId.Boolean;
    public override string Name => "bool";
    public override int BitWidth => 1;
    public override int ByteWidth => 1;
    public override bool IsSigned => false;

    public static void Visit(IVisitor visitor,Field field, ref bool value) => Accept(visitor, field, ref value);
    
}