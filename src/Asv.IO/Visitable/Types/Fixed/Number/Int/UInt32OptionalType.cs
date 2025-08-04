namespace Asv.IO;

public sealed class UInt32OptionalType(uint min = uint.MinValue, uint max = uint.MaxValue, uint? defaultValue = null)
    : IntegerType<UInt32OptionalType,uint?>(min, max, defaultValue)
{
    public const string TypeId = "uint32?";
    public static readonly UInt32OptionalType Default = new();
    public override string Name => TypeId;
}