namespace Asv.IO;

public sealed class DateOnlyOptionalType : FixedType<DateOnlyOptionalType,System.DateOnly?>
{
    public const string TypeId = "date?";
    public static readonly DateOnlyOptionalType Default = new();
    public override string Name => TypeId;
}