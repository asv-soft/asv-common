using System;
using DotNext;

namespace Asv.IO;

public class RandomizeVisitor(Random random, string allowedChars, int minStringSize, int maxStringSize, uint minListSize, uint maxListSize) 
    : IFullVisitor
{
    public const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    public const int MaxStringSize = 16;
    public const int MinStringSize = 0;
    public const uint MinListSize = 0;
    public const uint MaxListSize = 5;
    public static RandomizeVisitor Shared { get; } = new(Random.Shared, AllowedChars, MinStringSize, MaxStringSize, MinListSize, MaxListSize);

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

    public void Visit(Field field, ref string value) => value = random.NextString(allowedChars, random.Next(minStringSize, maxStringSize));
    
    public void Visit(Field field, ref bool value) => value = random.Next(0, 2) == 1;

    public void Visit(Field field, ref char value)
    {
        value = allowedChars[random.Next(0, allowedChars.Length)];
    }

    public void VisitUnknown(Field field)
    {
        // do nothing
    }

    public void BeginStruct(Field field)
    {
        // do nothing
    }

    public void BeginStruct(Field field, StructType type)
    {
        throw new NotImplementedException();
    }

    public void EndStruct()
    {
        // do nothing
    }
    
    public void BeginArray(Field field, int size)
    {
        // do nothing
    }

    public void BeginArray(Field field, ArrayType fieldType, int size)
    {
        throw new NotImplementedException();
    }

    public void EndArray()
    {
        // do nothing
    }

    public void BeginList(Field field, IFieldType type, ref uint size)
    {
        size = (uint)random.Next((int)minListSize, (int)maxListSize);
    }

    public void BeginList(Field field, ListType type, ref uint size)
    {
        throw new NotImplementedException();
    }

    public void EndList()
    {
        // do nothing
    }

    public void Visit(Field field, FloatType type, ref double value)
    {
        throw new NotImplementedException();
    }

    public void Visit(Field field, FloatType type, ref float value)
    {
        throw new NotImplementedException();
    }

    public void Visit(Field field, FloatType type, ref Half value)
    {
        throw new NotImplementedException();
    }

    public void Visit(Field field, Int8Type type, ref sbyte value)
    {
        throw new NotImplementedException();
    }

    public void Visit(Field field, Int16Type type, ref short value)
    {
        throw new NotImplementedException();
    }

    public void Visit(Field field, Int32Type type, ref int value)
    {
        throw new NotImplementedException();
    }

    public void Visit(Field field, Int64Type type, ref long value)
    {
        throw new NotImplementedException();
    }

    public void Visit(Field field, UInt8Type type, ref byte value)
    {
        throw new NotImplementedException();
    }

    public void Visit(Field field, UInt16Type type, ref ushort value)
    {
        throw new NotImplementedException();
    }

    public void Visit(Field field, UInt32Type type, ref uint value)
    {
        throw new NotImplementedException();
    }

    public void Visit(Field field, UInt64Type type, ref ulong value)
    {
        throw new NotImplementedException();
    }

    public void Visit(Field field, StringType type, ref string value)
    {
        throw new NotImplementedException();
    }

    public void Visit(Field field, BoolType type, ref bool value)
    {
        throw new NotImplementedException();
    }

    public void Visit(Field field, CharType type, ref char value)
    {
        throw new NotImplementedException();
    }

    public void VisitUnknown(Field field, IFieldType type)
    {
        throw new NotImplementedException();
    }
}