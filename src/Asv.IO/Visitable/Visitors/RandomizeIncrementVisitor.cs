using System;

namespace Asv.IO;

public class RandomizeIncrementVisitor(
    int index,
    int decimation,
    string allowedChars,
    bool skipUnknown = false
) : FullVisitorBase(skipUnknown)
{
    public const string AllowedChars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private static int Dec(int d) => d <= 0 ? 1 : d;

    private static int RangeOr1(int max, int min) => Math.Max(1, max - min); // для модуло

    private static uint RangeOr1(uint max, uint min) => Math.Max(1u, max - min);

    private static ushort RangeOr1(ushort max, ushort min) => (ushort)Math.Max(1, max - min);

    private static byte RangeOr1(byte max, byte min) => (byte)Math.Max(1, max - min);

    private static double Grid(double min, double max, int ix, int dec)
    {
        dec = Dec(dec);
        var delta = (max - min) / dec; // как в твоём DoubleOptionalType
        var step = ix % dec;
        return min + (delta * step);
    }

    private static float Grid(float min, float max, int ix, int dec) =>
        (float)Grid((double)min, (double)max, ix, dec);

    private static Half Grid(Half min, Half max, int ix, int dec) =>
        (Half)Grid((double)min, (double)max, ix, dec);

    private static sbyte StepInt(sbyte min, sbyte max, int ix)
    {
        var span = (int)RangeOr1(max, min);
        return (sbyte)(min + (ix % span));
    }

    private static short StepInt(short min, short max, int ix)
    {
        var span = (int)RangeOr1(max, min);
        return (short)(min + (ix % span));
    }

    private static int StepInt(int min, int max, int ix)
    {
        var span = RangeOr1(max, min);
        return min + (ix % span);
    }

    private static long StepInt(long min, long max, int ix)
    {
        var span = Math.Max(1L, max - min); // без +1, чтобы не переполнять
        return min + (ix % (int)span);
    }

    private static byte StepUInt(byte min, byte max, int ix)
    {
        var span = (int)RangeOr1(max, min);
        return (byte)(min + (ix % span));
    }

    private static ushort StepUInt(ushort min, ushort max, int ix)
    {
        var span = (int)RangeOr1(max, min);
        return (ushort)(min + (ix % span));
    }

    private static uint StepUInt(uint min, uint max, int ix)
    {
        var span = (uint)Math.Max(1, (long)max - min);
        return min + (uint)(ix % (int)span);
    }

    private static ulong StepUInt(ulong min, ulong max, int ix)
    {
        // работа с очень широким диапазоном без переполнений:
        var span = max >= min ? (max - min) : 0UL;
        if (span == 0)
        {
            return min;
        }

        return min + ((ulong)ix % span); // без +1, чтобы не переполнить при max=ULong.MaxValue
    }

    private static string StepString(string alphabet, uint minSize, uint maxSize, int ix)
    {
        var chars = string.IsNullOrEmpty(alphabet) ? AllowedChars : alphabet;
        var lenSpan = (int)Math.Max(1u, maxSize - minSize + 1u);
        var len = (int)minSize + (ix % lenSpan);
        if (len <= 0)
        {
            var stepString = string.Empty;
            if (stepString != null)
            {
                return stepString;
            }
        }

        var res = new char[len];
        for (int i = 0; i < len; i++)
        {
            // детерминированное "переборное" наполнение
            res[i] = chars[(i + ix) % chars.Length];
        }

        return new string(res);
    }

    private static DateTime StepDateTimeBase(int ix) =>
        new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(ix);

    private static TimeSpan StepTimeSpanBase(int ix) => TimeSpan.FromSeconds(ix);

    private static DateOnly StepDateOnlyBase(int ix)
    {
        // простая решётка в пределах валидных дат
        var baseDate = new DateOnly(2000, 1, 1);
        return baseDate.AddDays(ix);
    }

    private static TimeOnly StepTimeOnlyBase(int ix)
    {
        var sec = ix % (24 * 3600);
        var h = sec / 3600;
        var m = (sec % 3600) / 60;
        var s = sec % 60;
        return new TimeOnly(h, m, s);
    }

    public override void BeginList(Field field, ListType type, ref uint size)
    {
        var span = Math.Max(0u, type.MaxSize - type.MinSize);
        size = span == 0 ? (uint)type.MinSize : (uint)(type.MinSize + (index % (int)span));
    }

    public override void EndList() { }

    public override void Visit(Field field, DoubleOptionalType type, ref double? value) =>
        value =
            (index % 2 == 0)
                ? Grid(type.Min ?? double.MinValue, type.Max ?? double.MaxValue, index, decimation)
                : null;

    public override void Visit(Field field, FloatOptionalType type, ref float? value) =>
        value =
            (index % 2 == 0)
                ? Grid(type.Min ?? float.MinValue, type.Max ?? float.MaxValue, index, decimation)
                : null;

    public override void Visit(Field field, HalfFloatOptionalType type, ref Half? value) =>
        value =
            (index % 2 == 0)
                ? Grid(type.Min ?? Half.MinValue, type.Max ?? Half.MaxValue, index, decimation)
                : null;

    public override void Visit(Field field, Int8OptionalType type, ref sbyte? value) =>
        value =
            (index % 2 == 0)
                ? StepInt(type.Min ?? sbyte.MinValue, type.Max ?? sbyte.MaxValue, index)
                : null;

    public override void Visit(Field field, Int16OptionalType type, ref short? value) =>
        value =
            (index % 2 == 0)
                ? StepInt(type.Min ?? short.MinValue, type.Max ?? short.MaxValue, index)
                : null;

    public override void Visit(Field field, Int32OptionalType type, ref int? value) =>
        value =
            (index % 2 == 0)
                ? StepInt(type.Min ?? int.MinValue, type.Max ?? int.MaxValue, index)
                : null;

    public override void Visit(Field field, Int64OptionalType type, ref long? value) =>
        value =
            (index % 2 == 0)
                ? StepInt(type.Min ?? long.MinValue, type.Max ?? long.MaxValue, index)
                : null;

    public override void Visit(Field field, UInt8OptionalType type, ref byte? value) =>
        value =
            (index % 2 == 0)
                ? StepUInt(type.Min ?? byte.MinValue, type.Max ?? byte.MaxValue, index)
                : null;

    public override void Visit(Field field, UInt16OptionalType type, ref ushort? value) =>
        value =
            (index % 2 == 0)
                ? StepUInt(type.Min ?? ushort.MinValue, type.Max ?? ushort.MaxValue, index)
                : null;

    public override void Visit(Field field, UInt32OptionalType type, ref uint? value) =>
        value =
            (index % 2 == 0)
                ? StepUInt(type.Min ?? uint.MinValue, type.Max ?? uint.MaxValue, index)
                : null;

    public override void Visit(Field field, UInt64OptionalType type, ref ulong? value) =>
        value =
            (index % 2 == 0) ? StepUInt(type.Min ?? 0UL, type.Max ?? ulong.MaxValue, index) : null;

    public override void Visit(Field field, StringOptionalType type, ref string? value) =>
        value =
            (index % 2 == 0)
                ? StepString(type.AllowedChars ?? allowedChars, type.MinSize, type.MaxSize, index)
                : null;

    public override void Visit(Field field, BoolOptionalType type, ref bool? value) =>
        value = (index % 2 == 0) ? ((index / 2) % 2 == 0) : null; // детерминированный паттерн

    public override void Visit(Field field, CharOptionalType type, ref char? value)
    {
        if (index % 2 != 0)
        {
            value = null;
            return;
        }

        var val = type.AllowedChars ?? allowedChars;
        value = val[(index / 2) % val.Length];
    }

    public override void Visit(Field field, DateTimeOptionalType type, ref DateTime? value) =>
        value = (index % 2 == 0) ? StepDateTimeBase(index) : null;

    public override void Visit(Field field, TimeSpanOptionalType type, ref TimeSpan? value) =>
        value = (index % 2 == 0) ? StepTimeSpanBase(index) : null;

    public override void Visit(Field field, DateOnlyOptionalType type, ref DateOnly? value) =>
        value = (index % 2 == 0) ? StepDateOnlyBase(index) : null;

    public override void Visit(Field field, TimeOnlyOptionalType type, ref TimeOnly? value) =>
        value = (index % 2 == 0) ? StepTimeOnlyBase(index) : null;

    public override void Visit(Field field, DoubleType type, ref double value) =>
        value = Grid(type.Min, type.Max, index, decimation);

    public override void Visit(Field field, FloatType type, ref float value) =>
        value = Grid(type.Min, type.Max, index, decimation);

    public override void Visit(Field field, HalfFloatType type, ref Half value) =>
        value = Grid(type.Min, type.Max, index, decimation);

    public override void Visit(Field field, Int8Type type, ref sbyte value) =>
        value = StepInt(type.Min, type.Max, index);

    public override void Visit(Field field, Int16Type type, ref short value) =>
        value = StepInt(type.Min, type.Max, index);

    public override void Visit(Field field, Int32Type type, ref int value) =>
        value = StepInt(type.Min, type.Max, index);

    public override void Visit(Field field, Int64Type type, ref long value) =>
        value = StepInt(type.Min, type.Max, index);

    public override void Visit(Field field, UInt8Type type, ref byte value) =>
        value = StepUInt(type.Min, type.Max, index);

    public override void Visit(Field field, UInt16Type type, ref ushort value) =>
        value = StepUInt(type.Min, type.Max, index);

    public override void Visit(Field field, UInt32Type type, ref uint value) =>
        value = StepUInt(type.Min, type.Max, index);

    public override void Visit(Field field, UInt64Type type, ref ulong value) =>
        value = StepUInt(type.Min, type.Max, index);

    public override void Visit(Field field, StringType type, ref string value) =>
        value = StepString(type.AllowedChars ?? allowedChars, type.MinSize, type.MaxSize, index);

    public override void Visit(Field field, BoolType type, ref bool value) =>
        value = (index % 2) == 0;

    public override void Visit(Field field, CharType type, ref char value)
    {
        var val = type.AllowedChars ?? allowedChars;
        value = val[index % val.Length];
    }

    public override void Visit(Field field, DateTimeType type, ref DateTime value) =>
        value = StepDateTimeBase(index);

    public override void Visit(Field field, TimeSpanType type, ref TimeSpan value) =>
        value = StepTimeSpanBase(index);

    public override void Visit(Field field, DateOnlyType type, ref DateOnly value) =>
        value = StepDateOnlyBase(index);

    public override void Visit(Field field, TimeOnlyType type, ref TimeOnly value) =>
        value = StepTimeOnlyBase(index);

    public override void BeginArray(Field field, ArrayType fieldType) { }

    public override void EndArray() { }

    public override void BeginStruct(Field field, StructType type) { }

    public override void EndStruct() { }

    public override void BeginOptionalStruct(
        Field field,
        OptionalStructType type,
        bool isPresent,
        out bool createNew
    )
    {
        // детерминированно: создаём новый экземпляр каждый 3-й индекс (и если не было)
        createNew = (!isPresent) && (index % 3 == 0);
    }

    public override void EndOptionalStruct(bool isPresent) { }
}
