namespace Asv.IO;

public sealed class UInt32Type(
    uint min = uint.MinValue,
    uint max = uint.MaxValue,
    uint defaultValue = 0
) : IntegerType<UInt32Type, uint>(min, max, defaultValue)
{
    public const string TypeId = "uint32";
    public static readonly UInt32Type Default = new();
    public override string Name => TypeId;
}
