namespace Asv.IO;

public sealed class TimeSpanOptionalType : FixedType<TimeSpanOptionalType,System.TimeSpan?>
{
    public const string TypeId = "time-span?";
    public static readonly TimeSpanOptionalType Default = new();
    public override string Name => TypeId;
}