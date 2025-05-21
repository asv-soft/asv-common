namespace Asv.IO;

public sealed class Int8Type : IntegerType
{
    public static readonly Int8Type Default = new();
    public override FieldTypeId TypeId => FieldTypeId.Int8;
    public override string Name => "int8";
    public override int BitWidth => 8;
    public override int ByteWidth => 1;
    public override bool IsSigned => true;
    public static void Accept(IVisitor visitor,Field field, ref sbyte value) => FieldType.Accept(visitor, field, ref value);
    
    
}

