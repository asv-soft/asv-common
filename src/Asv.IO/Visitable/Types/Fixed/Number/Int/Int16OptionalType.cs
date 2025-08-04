namespace Asv.IO;

public sealed class Int16OptionalType(short min = short.MinValue, short max = short.MaxValue, short? defaultValue = null) 
    : IntegerType<Int16OptionalType,short?>(min, max, defaultValue)
{
    public const string TypeId = "int16?";
    public static readonly Int16OptionalType Default = new();
    public override string Name => TypeId;
}