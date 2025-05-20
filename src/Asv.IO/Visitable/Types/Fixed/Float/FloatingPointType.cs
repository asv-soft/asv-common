namespace Asv.IO;

public abstract class FloatingPointType: NumberType
{
    public enum PrecisionKind
    {
        Half,
        Single,
        Double
    }

    public abstract PrecisionKind Precision { get; }
}