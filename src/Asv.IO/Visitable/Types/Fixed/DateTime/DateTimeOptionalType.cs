namespace Asv.IO;

public sealed class DateTimeOptionalType : FixedType<DateTimeOptionalType, System.DateTime?>
{
    public const string TypeId = "date-time?";
    public static readonly DateTimeOptionalType Default = new();
    public override string Name => TypeId;
}
