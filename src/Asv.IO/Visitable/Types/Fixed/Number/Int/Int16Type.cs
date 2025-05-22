namespace Asv.IO;

public sealed class Int16Type(short min = short.MinValue, short max = short.MaxValue) 
    : IntegerType<Int16Type,short>(min, max)
{
    public const string TypeId = "int16";
    public static readonly Int16Type Default = new();
    public override string Name => TypeId;
}