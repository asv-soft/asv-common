namespace Asv.IO;

public sealed class Int64Type(
    long min = long.MinValue,
    long max = long.MaxValue,
    long defaultValue = 0
) : IntegerType<Int64Type, long>(min, max, defaultValue)
{
    public const string TypeId = "int64";
    public static readonly Int64Type Default = new();
    public override string Name => TypeId;
}
