namespace Asv.IO;

public sealed class TimeOnlyOptionalType : FixedType<TimeOnlyOptionalType,System.TimeOnly?>
{
    public const string TypeId = "time?";
    public static readonly TimeOnlyOptionalType Default = new();
    public override string Name => TypeId;
}