namespace Asv.IO;

public sealed class Int64OptionalType(long min = long.MinValue, long max = long.MaxValue, long? defaultValue = null) 
    : IntegerType<Int64OptionalType,long?>(min, max, defaultValue)
{
    public const string TypeId = "int64?";
    public static readonly Int64OptionalType Default = new();
    public override string Name => TypeId;
}