namespace Asv.IO;

public sealed class DateTimeType : FixedType<DateTimeType, System.DateTime>
{
    public const string TypeId = "date-time";
    public static readonly DateTimeType Default = new();
    public override string Name => TypeId;
}
