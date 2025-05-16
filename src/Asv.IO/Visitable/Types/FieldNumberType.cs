using System.Numerics;

namespace Asv.IO;

public abstract class FieldNumberType: FieldFixedWidthType
{
    public abstract bool IsSigned { get; }
}

