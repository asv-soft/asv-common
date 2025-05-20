namespace Asv.IO;

public sealed class NullType : FieldType
{
    public static readonly NullType Default = new NullType();

    public override FieldTypeId TypeId => FieldTypeId.Null;
    public override string Name => "null";
}