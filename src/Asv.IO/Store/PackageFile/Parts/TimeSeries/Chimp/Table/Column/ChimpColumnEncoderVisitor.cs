using System;

namespace Asv.IO;

public class ChimpColumnEncoderVisitor(ChimpEncoder[] streams) : ChimpVisitorBase
{
    private int _index;

    public void Reset()
    {
        _index = 0;
    }

    public override void Visit(Field field, DoubleType type, ref double value)
    {
        streams[_index++].Add(BitConverter.DoubleToUInt64Bits(value));
    }

    public override void Visit(Field field, FloatType type, ref float value)
    {
        streams[_index++].Add(BitConverter.DoubleToUInt64Bits(value));
    }

    public override void Visit(Field field, Int8Type type, ref sbyte value)
    {
        streams[_index++].Add(unchecked((ulong)value));
    }

    public override void Visit(Field field, Int16Type type, ref short value)
    {
        streams[_index++].Add(unchecked((ulong)value));
    }

    public override void Visit(Field field, Int32Type type, ref int value)
    {
        streams[_index++].Add(unchecked((ulong)value));
    }

    public override void Visit(Field field, Int64Type type, ref long value)
    {
        streams[_index++].Add(unchecked((ulong)value));
    }

    public override void Visit(Field field, UInt8Type type, ref byte value)
    {
        streams[_index++].Add(value);
    }

    public override void Visit(Field field, UInt16Type type, ref ushort value)
    {
        streams[_index++].Add(value);
    }

    public override void Visit(Field field, UInt32Type type, ref uint value)
    {
        streams[_index++].Add(value);
    }

    public override void Visit(Field field, UInt64Type type, ref ulong value)
    {
        streams[_index++].Add(value);
    }

    public override void Visit(Field field, CharType type, ref char value)
    {
        streams[_index++].Add(value);
    }

    public override void Visit(Field field, BoolType type, ref bool value)
    {
        streams[_index++].Add(value ? 1U : 0U);
    }
}
