namespace Asv.IO;

public sealed class Int8OptionalType(sbyte min = sbyte.MinValue, sbyte max = sbyte.MaxValue, sbyte? defaultValue = null) 
    : IntegerType<Int8OptionalType,sbyte?>(min, max, defaultValue)
{
    public const string TypeId = "int8?";
    public static readonly Int8OptionalType Default = new();
    public override string Name => TypeId;
}