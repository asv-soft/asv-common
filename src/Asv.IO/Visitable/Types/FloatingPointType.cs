namespace Asv.IO;

public abstract class FloatingPointType: FieldNumberType
{
    public enum PrecisionKind
    {
        Half,
        Single,
        Double
    }

    public abstract PrecisionKind Precision { get; }
}