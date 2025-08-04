namespace Asv.IO;

public sealed class BoolOptionalType: FixedType<BoolOptionalType,bool?>
{
    public const string TypeId = "bool?";
    public static readonly BoolOptionalType Default = new();
    public override string Name => TypeId;
}