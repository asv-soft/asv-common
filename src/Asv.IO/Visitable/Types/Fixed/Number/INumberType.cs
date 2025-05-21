namespace Asv.IO;

public interface INumberType : IFixedType
{
}

public abstract class NumberType<TSelf, TValue>(TValue min, TValue max): FixedType<TSelf, TValue>, INumberType 
    where TSelf : IFieldType
{
    public TValue Max => max;
    public TValue Min => min;
}


