namespace Asv.IO;

public sealed class AsciiCharType : FixedWidthType
{
    public static readonly AsciiCharType Default = new();

    public override FieldTypeId TypeId => FieldTypeId.AsciiChar;
    public override string Name => "ascii-char";
    public override int BitWidth => 8;
    public override int ByteWidth => 1; 
    public static void Accept(IVisitor visitor, Field field, ref char value) => FieldType.Accept(visitor, field, ref value);
}