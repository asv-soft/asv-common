namespace Asv.IO;

public sealed class FloatOptionalType(float min = float.MinValue, float max = float.MaxValue, float? defaultValue = null)
    : FloatingPointType<FloatOptionalType,float?>(min, max, defaultValue)
{
    public const string TypeId = "float?";
    public static readonly FloatOptionalType Default = new();
    public override string Name => TypeId;
    public override PrecisionKind Precision => PrecisionKind.Single;
}