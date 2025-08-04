namespace Asv.IO;

public sealed class UInt64OptionalType (ulong min = ulong.MinValue, ulong max = ulong.MaxValue, ulong? defaultValue = null)
    : IntegerType<UInt64OptionalType,ulong?>(min, max, defaultValue)
{
    public const string TypeId = "uint64?";
    public static readonly UInt64OptionalType Default = new();
    public override string Name => TypeId;
}