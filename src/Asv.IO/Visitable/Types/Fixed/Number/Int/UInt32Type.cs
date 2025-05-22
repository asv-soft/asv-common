namespace Asv.IO;

public sealed class UInt32Type(uint min = uint.MinValue, uint max = uint.MaxValue) 
    : IntegerType<UInt32Type,uint>(min, max)
{
    public const string TypeId = "uint32";
    public static readonly UInt32Type Default = new();
    public override string Name => TypeId;
}