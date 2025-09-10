using System;
using DotNext;

namespace Asv.IO;

public class RandomizeVisitor(Random random, string allowedChars, bool skipUnknown = false)
    : FullVisitorBase(skipUnknown)
{
    public const string AllowedChars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    public static RandomizeVisitor Shared { get; } = new(Random.Shared, AllowedChars);

    public override void BeginList(Field field, ListType type, ref uint size)
    {
        size = (uint)random.Next(type.MinSize, type.MaxSize);
    }

    public override void EndList()
    {
        // do nothing
    }

    public override void Visit(Field field, DoubleOptionalType type, ref double? value)
    {
        if (random.NextBoolean())
        {
            value = type.Min + ((type.Max - type.Min) * random.NextDouble());
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, FloatOptionalType type, ref float? value)
    {
        if (random.NextBoolean())
        {
            value = type.Min + ((type.Max - type.Min) * (float)random.NextDouble());
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, HalfFloatOptionalType type, ref Half? value)
    {
        if (random.NextBoolean())
        {
            value = type.Min + ((type.Max - type.Min) * (Half)random.NextDouble());
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, Int8OptionalType type, ref sbyte? value)
    {
        if (random.NextBoolean())
        {
            value = (sbyte)random.Next(type.Min ?? sbyte.MinValue, type.Max ?? sbyte.MaxValue);
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, Int16OptionalType type, ref short? value)
    {
        if (random.NextBoolean())
        {
            value = (short)random.Next(type.Min ?? short.MinValue, type.Max ?? short.MaxValue);
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, Int32OptionalType type, ref int? value)
    {
        if (random.NextBoolean())
        {
            value = random.Next(type.Min ?? int.MinValue, type.Max ?? int.MaxValue);
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, Int64OptionalType type, ref long? value)
    {
        if (random.NextBoolean())
        {
            value = type.Min + (long?)(random.NextDouble() * (type.Max - type.Min + 1));
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, UInt8OptionalType type, ref byte? value)
    {
        if (random.NextBoolean())
        {
            value = (byte)random.Next(type.Min ?? byte.MinValue, type.Max ?? byte.MaxValue);
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, UInt16OptionalType type, ref ushort? value)
    {
        if (random.NextBoolean())
        {
            value = (ushort)random.Next(type.Min ?? ushort.MinValue, type.Max ?? ushort.MaxValue);
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, UInt32OptionalType type, ref uint? value)
    {
        if (random.NextBoolean())
        {
            value = type.Min + (uint?)(random.NextDouble() * (type.Max - type.Min + 1));
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, UInt64OptionalType type, ref ulong? value)
    {
        if (random.NextBoolean())
        {
            value = type.Min + (ulong?)(random.NextDouble() * (type.Max - type.Min + 1));
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, StringOptionalType type, ref string? value)
    {
        if (random.NextBoolean())
        {
            var val = type.AllowedChars ?? allowedChars;
            value = random.NextString(val, random.Next((int)type.MinSize, (int)type.MaxSize));
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, BoolOptionalType type, ref bool? value)
    {
        if (random.NextBoolean())
        {
            value = random.NextBoolean();
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, CharOptionalType type, ref char? value)
    {
        if (random.NextBoolean())
        {
            var val = type.AllowedChars ?? allowedChars;
            value = val[random.Next(0, val.Length)];
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, DateTimeType type, ref DateTime value)
    {
        var minTicks = DateTime.MinValue.Ticks;
        var maxTicks = DateTime.MaxValue.Ticks;
        var randomTicks = (long)(minTicks + ((maxTicks - minTicks) * random.NextDouble()));
        value = new DateTime(randomTicks, DateTimeKind.Utc);
    }

    public override void Visit(Field field, DateTimeOptionalType type, ref DateTime? value)
    {
        if (random.NextBoolean())
        {
            var minTicks = DateTime.MinValue.Ticks;
            var maxTicks = DateTime.MaxValue.Ticks;
            var randomTicks = (long)(minTicks + ((maxTicks - minTicks) * random.NextDouble()));
            value = new DateTime(randomTicks, DateTimeKind.Utc);
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, TimeSpanType type, ref TimeSpan value)
    {
        var minTicks = TimeSpan.MinValue.Ticks;
        var maxTicks = TimeSpan.MaxValue.Ticks;
        var randomTicks = (long)(minTicks + ((maxTicks - minTicks) * random.NextDouble()));
        value = new TimeSpan(randomTicks);
    }

    public override void Visit(Field field, TimeSpanOptionalType type, ref TimeSpan? value)
    {
        if (random.NextBoolean())
        {
            var minTicks = TimeSpan.MinValue.Ticks;
            var maxTicks = TimeSpan.MaxValue.Ticks;
            var randomTicks = (long)(minTicks + ((maxTicks - minTicks) * random.NextDouble()));
            value = new TimeSpan(randomTicks);
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, DateOnlyType type, ref DateOnly value)
    {
        value = new DateOnly(random.Next(1, 9999), random.Next(1, 12), random.Next(1, 28));
    }

    public override void Visit(Field field, DateOnlyOptionalType type, ref DateOnly? value)
    {
        if (random.NextBoolean())
        {
            value = new DateOnly(random.Next(1, 9999), random.Next(1, 12), random.Next(1, 28));
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, TimeOnlyType type, ref TimeOnly value)
    {
        var hours = random.Next(0, 24);
        var minutes = random.Next(0, 60);
        var seconds = random.Next(0, 60);
        value = new TimeOnly(hours, minutes, seconds);
    }

    public override void Visit(Field field, TimeOnlyOptionalType type, ref TimeOnly? value)
    {
        if (random.NextBoolean())
        {
            var hours = random.Next(0, 24);
            var minutes = random.Next(0, 60);
            var seconds = random.Next(0, 60);
            value = new TimeOnly(hours, minutes, seconds);
        }
        else
        {
            value = null;
        }
    }

    public override void Visit(Field field, DoubleType type, ref double value)
    {
        value = type.Min + ((type.Max - type.Min) * random.NextDouble());
    }

    public override void Visit(Field field, FloatType type, ref float value)
    {
        value = (float)(type.Min + ((type.Max - type.Min) * random.NextDouble()));
    }

    public override void Visit(Field field, HalfFloatType type, ref Half value)
    {
        value = (Half)(type.Min + ((type.Max - type.Min) * (Half)random.NextDouble()));
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

    public override void BeginArray(Field field, ArrayType fieldType)
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

    public override void BeginOptionalStruct(
        Field field,
        OptionalStructType type,
        bool isPresent,
        out bool createNew
    )
    {
        createNew = random.NextBoolean();
    }

    public override void EndOptionalStruct(bool isPresent)
    {
        // do nothing
    }
}
