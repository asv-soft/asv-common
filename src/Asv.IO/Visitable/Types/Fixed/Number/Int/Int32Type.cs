namespace Asv.IO;

public sealed class Int32Type(int min = int.MinValue, int max = int.MaxValue, int defaultValue = 0)
    : IntegerType<Int32Type,int>(min, max, defaultValue)
{
    public const string TypeId = "int32";
    public static readonly Int32Type Default = new();
    public override string Name => TypeId;
}