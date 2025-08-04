namespace Asv.IO;

public sealed class DoubleOptionalType(double min = double.MinValue, double max = double.MaxValue, double? defaultValue = null) 
    : FloatingPointType<DoubleOptionalType,double?>(min, max, defaultValue)
{
    public const string TypeId = "double?";
    public static readonly DoubleOptionalType Default = new();
    public override string Name => TypeId;
    public override PrecisionKind Precision => PrecisionKind.Double;
}