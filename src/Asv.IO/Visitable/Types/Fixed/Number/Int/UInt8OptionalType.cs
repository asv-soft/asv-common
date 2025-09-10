namespace Asv.IO;

public sealed class UInt8OptionalType(
    byte min = byte.MinValue,
    byte max = byte.MaxValue,
    byte? defaultValue = null
) : IntegerType<UInt8OptionalType, byte?>(min, max, defaultValue)
{
    public const string TypeId = "uint8?";
    public static readonly UInt8OptionalType Default = new();
    public override string Name => TypeId;
}
