namespace Asv.IO;

public abstract class FieldFixedWidthType: FieldType
{
    public override bool IsFixedWidth => true;

    public abstract int BitWidth { get; }
}