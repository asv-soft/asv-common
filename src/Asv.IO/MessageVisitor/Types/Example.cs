namespace Asv.IO.MessageVisitor;

public class Example : IVisitable
{
    public static readonly Int8T.Field Value1Field = new Int8T.Builder()
        .Name(nameof(Value1Field))
        .Title("Title message field 1")
        .Description("Description message field 1")
        .Units("m")
        .FormatString("{0:0.00}")
        .Max(10)
        .Min(-10)
        .Build();
    
    public static readonly ArrayT.Field Value2Field = new ArrayT.Builder()
        .Name(nameof(Value1Field))
        .Title("Title message field 1")
        .Description("Description message field 1")
        .Units("m")
        .FormatString("{0:0.00}")
        .Build();

    private sbyte _value1;
    private sbyte[] _value2 = new sbyte[10];

    public void Accept(IMessageVisitor visitor)
    {
        Int8T.Accept(visitor, Value1Field, Value1Field.FieldType, ref _value1);
        ArrayT.Accept(visitor, Value2Field, Value2Field.FieldType, _value2.Length, (i, f, t, v) =>
        {
           Int8T.Accept(v, f, t, ref _value2[i]);
        });
    }
}