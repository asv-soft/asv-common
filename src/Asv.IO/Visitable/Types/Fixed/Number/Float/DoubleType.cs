namespace Asv.IO;

public sealed class DoubleType(double min = double.MinValue, double max = double.MaxValue) 
    : FloatingPointType<DoubleType,double>(min, max)
{
    public const string TypeId = "double";
    public static readonly DoubleType Default = new();
    public override string Name => TypeId;
    public override PrecisionKind Precision => PrecisionKind.Double;
}