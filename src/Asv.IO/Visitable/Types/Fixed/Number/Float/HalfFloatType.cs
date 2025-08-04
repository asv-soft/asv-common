using System;

namespace Asv.IO;

public sealed class HalfFloatType(Half min , Half max, Half defaultValue) 
    : FloatingPointType<HalfFloatType,Half>(min, max, defaultValue)
{
    public const string TypeId = "half";
    public static readonly HalfFloatType Default = new(Half.MinValue,Half.MaxValue, Half.NaN);
    public override string Name => TypeId;
    public override PrecisionKind Precision => PrecisionKind.Half;
}

public sealed class HalfFloatOptionalType(Half min , Half max, Half? defaultValue = null) 
    : FloatingPointType<HalfFloatOptionalType,Half?>(min, max, defaultValue)
{
    public const string TypeId = "half?";
    public static readonly HalfFloatOptionalType Default = new(Half.MinValue,Half.MaxValue, Half.NaN);
    public override string Name => TypeId;
    public override PrecisionKind Precision => PrecisionKind.Half;
}