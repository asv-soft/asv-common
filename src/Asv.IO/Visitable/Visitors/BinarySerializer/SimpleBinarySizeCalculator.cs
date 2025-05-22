using System;

namespace Asv.IO;

public class SimpleBinarySizeCalculator(bool skipUnknown) : FullVisitorBase(skipUnknown)
{
    public int Size { get; private set; }

    public override void Visit(Field field, UInt8Type type,  ref byte value)
    {
        Size += sizeof(byte);
    }

    public override void Visit(Field field, HalfFloatType type, ref Half value)
    {
        throw new NotImplementedException();
    }

    public override void Visit(Field field, Int8Type type, ref sbyte value)
    {
        Size += sizeof(sbyte);
    }

    public override void Visit(Field field, Int16Type type, ref short value)
    {
        Size += sizeof(short);
    }

    public override void Visit(Field field, UInt16Type type, ref ushort value)
    {
        Size += sizeof(ushort);
    }

    public override void Visit(Field field, Int32Type type, ref int value)
    {
        Size += sizeof(int);
    }

    public override void Visit(Field field, UInt32Type type, ref uint value)
    {
        Size += sizeof(uint);
    }

    public override void Visit(Field field, Int64Type type, ref long value)
    {
        Size += sizeof(long);
    }

    public override void Visit(Field field, UInt64Type type, ref ulong value)
    {
        Size += sizeof(ulong);
    }

    public override void Visit(Field field, FloatType type, ref float value)
    {
        Size += sizeof(float);
    }

    public override void Visit(Field field, DoubleType type, ref double value)
    {
        Size += sizeof(double);
    }

    public override void Visit(Field field, StringType type, ref string value)
    {
        Size+= BinSerialize.GetSizeForString(value);
    }

    public override void Visit(Field field, BoolType type, ref bool value)
    {
        Size += sizeof(bool);
    }

    public override void Visit(Field field, CharType type, ref char value)
    {
        if (type.Encoding == EncodingId.Ascii)
        {
            Size += 1;    
        }
        else
        {
            throw new NotImplementedException($"Encoding {type.Encoding} is not supported");
        }
        
    }

    public override void BeginArray(Field field, ArrayType type, int size)
    {
        // fixed size array => skip
    }

    public override void EndArray()
    {
        // fixed size array => skip
    }

    public override void BeginStruct(Field field, StructType type)
    {
        // fixed size struct => skip
    }

    public override void EndStruct()
    {
        // fixed size struct => skip
    }

    public override void BeginList(Field field, ListType type, ref uint size)
    {
        Size += sizeof(uint);
    }

    public override void EndList()
    {
        // do nothing
    }
}