namespace Asv.IO;

public sealed class UInt64Type(
    ulong min = ulong.MinValue,
    ulong max = ulong.MaxValue,
    ulong defaultValue = 0
) : IntegerType<UInt64Type, ulong>(min, max, defaultValue)
{
    public const string TypeId = "uint64";
    public static readonly UInt64Type Default = new();
    public override string Name => TypeId;
}
