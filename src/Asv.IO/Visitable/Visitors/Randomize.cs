using System;
using DotNext;

namespace Asv.IO;

public static class RandomizeVisitorMixin
{
    public static T Randomize<T>(this T src, RandomizeVisitor visitor)
        where T : IVisitable, new()
    {
        src.Accept(visitor);
        return src;
    }
    
    public static T Randomize<T>(this T src, Random random, string? allowedChars = null, int? maxStringSize = null)
        where T : IVisitable, new() =>
        src.Randomize(new RandomizeVisitor(random, allowedChars ?? RandomizeVisitor.AllowedChars, maxStringSize ?? RandomizeVisitor.MaxStringSize));

    public static T Randomize<T>(this T src, int seed, string? allowedChars = null, int? maxStringSize = null)
        where T : IVisitable, new() =>
        src.Randomize(new RandomizeVisitor(new Random(seed), allowedChars ?? RandomizeVisitor.AllowedChars , maxStringSize ?? RandomizeVisitor.MaxStringSize));
    
    public static T Randomize<T>(this T src)
        where T : IVisitable, new() =>
        src.Randomize(RandomizeVisitor.Shared);
}

public class RandomizeVisitor(Random random, string allowedChars, int maxStringSize) : IFullVisitor
{
    public const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    public const int MaxStringSize = 16;
    public static RandomizeVisitor Shared { get; } = new(Random.Shared, AllowedChars, MaxStringSize);

    public void Visit(Field field, ref int value) => value = random.Next();

    public void Visit(Field field, ref short value) => value = random.Next<short>();

    public void Visit(Field field, ref byte value) => value = random.Next<byte>();

    public void Visit(Field field, ref sbyte value) => value = random.Next<sbyte>();

    public void Visit(Field field, ref ushort value) => value = random.Next<ushort>();

    public void Visit(Field field, ref uint value) => value = random.Next<uint>();

    public void Visit(Field field, ref long value) => value = random.Next<long>();
    
    public void Visit(Field field, ref ulong value) => value = random.Next<ulong>();

    public void Visit(Field field, ref float value) => value = random.Next<float>();

    public void Visit(Field field, ref double value) => value = random.Next<double>();

    public void Visit(Field field, ref string value) => value = random.NextString(allowedChars, maxStringSize);
    
    public void Visit(Field field, ref bool value) => value = random.Next(0, 2) == 1;

    public void VisitUnknown(Field field)
    {
        // do nothing
    }

    public void BeginStruct(Field field)
    {
        // do nothing
    }

    public void EndStruct()
    {
        // do nothing
    }
    
    public void BeginArray(Field field, int size)
    {
        // do nothing
    }

    public void EndArray()
    {
        // do nothing
    }
}