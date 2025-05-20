namespace Asv.IO;

public abstract class NumberType: FixedWidthType
{
    public abstract bool IsSigned { get; }
}

