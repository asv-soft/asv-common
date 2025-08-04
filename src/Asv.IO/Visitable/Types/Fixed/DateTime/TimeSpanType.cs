namespace Asv.IO;

public sealed class TimeSpanType : FixedType<TimeSpanType,System.TimeSpan>
{
    public const string TypeId = "time-span";
    public static readonly TimeSpanType Default = new();
    public override string Name => TypeId;
}