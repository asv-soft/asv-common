namespace Asv.IO;

public sealed class BoolType: FixedType<BoolType,bool>
{
    public const string TypeId = "bool";
    public static readonly BoolType Default = new();
    public override string Name => TypeId;
}