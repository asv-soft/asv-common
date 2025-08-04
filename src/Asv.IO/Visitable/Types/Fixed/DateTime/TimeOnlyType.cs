namespace Asv.IO;

public sealed class TimeOnlyType : FixedType<TimeOnlyType,System.TimeOnly>
{
    public const string TypeId = "time";
    public static readonly TimeOnlyType Default = new();
    public override string Name => TypeId;
}