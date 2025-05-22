using System;

namespace Asv.IO;

public sealed class Int8Type(sbyte min = sbyte.MinValue, sbyte max = sbyte.MaxValue) 
    : IntegerType<Int8Type,sbyte>(min, max)
{
    public const string TypeId = "int8";
    public static readonly Int8Type Default = new();
    public override string Name => TypeId;
}

