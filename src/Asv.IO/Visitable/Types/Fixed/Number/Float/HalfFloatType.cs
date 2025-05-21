using System;

namespace Asv.IO;

public sealed class HalfFloatType(Half min, Half max) 
    : FloatingPointType<FloatType,Half>(max, min)
{
    public const string TypeId = "halffloat";
    public static readonly HalfFloatType Default = new(Half.MinValue,Half.MaxValue);
    public override string Name => TypeId;
    public override PrecisionKind Precision => PrecisionKind.Half;
}