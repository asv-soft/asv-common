namespace Asv.IO
{
    public sealed class Int64Type : IntegerType
    {
        public static readonly Int64Type Default = new();

        public override FieldTypeId TypeId => FieldTypeId.Int64;
        public override string Name => "int64";
        public override int BitWidth => 64;
        public override int ByteWidth => 8;

        public override bool IsSigned => true;
        
        public static void Accept(IVisitor visitor,Field field, ref long value) => FieldType.Accept(visitor, field, ref value);
    }
}