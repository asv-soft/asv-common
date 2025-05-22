namespace Asv.IO;

public sealed class FloatType(float min = float.MinValue, float max = float.MaxValue) 
    : FloatingPointType<FloatType,float>(min, max)
{
    public const string TypeId = "float";
    public static readonly FloatType Default = new();
    public override string Name => TypeId;
    public override PrecisionKind Precision => PrecisionKind.Single;
}