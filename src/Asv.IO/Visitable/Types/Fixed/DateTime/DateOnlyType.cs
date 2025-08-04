namespace Asv.IO;

public sealed class DateOnlyType : FixedType<DateOnlyType,System.DateOnly>
{
    public const string TypeId = "date";
    public static readonly DateOnlyType Default = new();
    public override string Name => TypeId;
}