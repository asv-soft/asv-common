namespace Asv.IO;

public sealed class UInt64Type (ulong min = ulong.MinValue, ulong max = ulong.MaxValue) 
    : IntegerType<UInt64Type,ulong>(min, max)
{
    public const string TypeId = "uint64";
    public static readonly UInt64Type Default = new();
    public override string Name => TypeId;
}