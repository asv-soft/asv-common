namespace Asv.IO;

public sealed class UInt16OptionalType(ushort min = ushort.MinValue, ushort max = ushort.MaxValue, ushort? defaultValue = null)
    : IntegerType<UInt16OptionalType,ushort?>(min, max, defaultValue)
{
    public const string TypeId = "uint16?";
    public static readonly UInt16OptionalType Default = new();
    public override string Name => TypeId;
}