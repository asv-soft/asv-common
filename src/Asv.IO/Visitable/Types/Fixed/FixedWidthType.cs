namespace Asv.IO;

public abstract class FixedWidthType: FieldType
{
    public override bool IsFixedWidth => true;

    public abstract int BitWidth { get; }
    public abstract int ByteWidth { get; }
}