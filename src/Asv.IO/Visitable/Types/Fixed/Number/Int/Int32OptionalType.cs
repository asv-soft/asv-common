namespace Asv.IO;

public sealed class Int32OptionalType(int min = int.MinValue, int max = int.MaxValue, int? defaultValue = null)
    : IntegerType<Int32OptionalType,int?>(min, max, defaultValue)
{
    public const string TypeId = "int32?";
    public static readonly Int32OptionalType Default = new();
    public override string Name => TypeId;
}