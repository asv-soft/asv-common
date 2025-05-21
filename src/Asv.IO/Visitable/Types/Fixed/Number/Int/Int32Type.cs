namespace Asv.IO;

public sealed class Int32Type(int min = int.MinValue, int max = int.MaxValue) 
    : IntegerType<Int32Type,int>(max, min)
{
    public const string TypeId = "int32";
    public static readonly Int32Type Default = new();
    public override string Name => TypeId;
}

