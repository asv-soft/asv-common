namespace Asv.IO;

public interface IFloatingPointType : IFixedType
{
}


public abstract class FloatingPointType<TSelf, TValue>(TValue min, TValue max) 
    : NumberType<TSelf, TValue>(min, max), IFloatingPointType
    where TSelf : IFieldType, IFloatingPointType
{
    public enum PrecisionKind
    {
        Half,
        Single,
        Double
    }

    public abstract PrecisionKind Precision { get; }
}