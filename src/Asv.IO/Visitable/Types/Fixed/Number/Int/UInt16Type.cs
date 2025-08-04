namespace Asv.IO;

public sealed class UInt16Type(ushort min = ushort.MinValue, ushort max = ushort.MaxValue, ushort defaultValue = 0)
    : IntegerType<UInt16Type,ushort>(min, max, defaultValue)
{
    public const string TypeId = "uint16";
    public static readonly UInt16Type Default = new();
    public override string Name => TypeId;
}
