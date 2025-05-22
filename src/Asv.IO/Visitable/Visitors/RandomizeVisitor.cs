using System;
using DotNext;

namespace Asv.IO;

public class RandomizeVisitor(Random random, string allowedChars, bool skipUnknown = false) 
    : FullVisitorBase(skipUnknown)
{
    public const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    public static RandomizeVisitor Shared { get; } = new(Random.Shared, AllowedChars);

    public override void BeginList(Field field, ListType type, ref uint size)
    {
        size = (uint)random.Next(type.MinSize, type.MaxSize);
    }

    public override void EndList()
    {
        // do nothing
    }

    public override void Visit(Field field, DoubleType type, ref double value)
    {
        value = type.Min + (type.Max - type.Min) * random.NextDouble();
    }

    public override void Visit(Field field, FloatType type, ref float value)
    {
        value = (float)(type.Min + (type.Max - type.Min) * random.NextDouble());
    }

    public override void Visit(Field field, HalfFloatType type, ref Half value)
    {
        value = (Half)(type.Min + (type.Max - type.Min) * (Half)random.NextDouble());
    }

    public override void Visit(Field field, Int8Type type, ref sbyte value)
    {
        value = (sbyte)random.Next(type.Min, type.Max);
    }

    public override void Visit(Field field, Int16Type type, ref short value)
    {
        value = (short)random.Next(type.Min, type.Max);
    }

    public override void Visit(Field field, Int32Type type, ref int value)
    {
        value = random.Next(type.Min, type.Max);
    }

    public override void Visit(Field field, Int64Type type, ref long value)
    {
        value = type.Min + (long)(random.NextDouble() * (type.Max - type.Min + 1));
    }

    public override void Visit(Field field, UInt8Type type, ref byte value)
    {
        value = (byte)random.Next(type.Min, type.Max);
    }

    public override void Visit(Field field, UInt16Type type, ref ushort value)
    {
        value = (ushort)random.Next(type.Min, type.Max);
    }

    public override void Visit(Field field, UInt32Type type, ref uint value)
    {
        value = type.Min + (uint)(random.NextDouble() * (type.Max - type.Min + 1));
    }

    public override void Visit(Field field, UInt64Type type, ref ulong value)
    {
        value = type.Min + (ulong)(random.NextDouble() * (type.Max - type.Min + 1));
    }

    public override void Visit(Field field, StringType type, ref string value)
    {
        var val = type.AllowedChars ?? allowedChars;
        value = random.NextString(val, random.Next((int)type.MinSize, (int)type.MaxSize));
    }

    public override void Visit(Field field, BoolType type, ref bool value)
    {
        value = random.Next(0, 2) == 1;
    }

    public override void Visit(Field field, CharType type, ref char value)
    {
        var val = type.AllowedChars ?? allowedChars;
        value = val[random.Next(0, val.Length)];
    }

    public override void BeginArray(Field field, ArrayType fieldType, int size)
    {
        // do nothing
    }

    public override void EndArray()
    {
        // do nothing
    }

    public override void BeginStruct(Field field, StructType type)
    {
        // do nothing
    }

    public override void EndStruct()
    {
        // do nothing
    }
}