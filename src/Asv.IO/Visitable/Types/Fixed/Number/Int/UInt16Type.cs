namespace Asv.IO;

public sealed class UInt16Type(ushort min = ushort.MinValue, ushort max = ushort.MaxValue) 
    : IntegerType<UInt16Type,ushort>(min, max)
{
    public const string TypeId = "uint16";
    public static readonly UInt16Type Default = new();
    public override string Name => TypeId;
}