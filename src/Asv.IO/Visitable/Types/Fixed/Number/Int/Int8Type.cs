namespace Asv.IO;

public sealed class Int8Type(
    sbyte min = sbyte.MinValue,
    sbyte max = sbyte.MaxValue,
    sbyte defaultValue = 0
) : IntegerType<Int8Type, sbyte>(min, max, defaultValue)
{
    public const string TypeId = "int8";
    public static readonly Int8Type Default = new();
    public override string Name => TypeId;
}
