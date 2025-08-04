namespace Asv.IO;

public sealed class UInt8Type(byte min = byte.MinValue, byte max = byte.MaxValue, byte defaultValue = 0)
    : IntegerType<UInt8Type,byte>(min, max, defaultValue)
{
    public const string TypeId = "uint8";
    public static readonly UInt8Type Default = new();
    public override string Name => TypeId;
}