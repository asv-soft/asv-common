using System;

namespace Asv.IO;

public class ChimpColumnDecoderVisitor(ChimpDecoder[] streams) : ChimpVisitorBase
{
    private int _index;

    public void Reset()
    {
        _index = 0;
    }

    public override void Visit(Field field, DoubleType type, ref double value)
    {
        value = BitConverter.UInt64BitsToDouble(streams[_index++].ReadNext());
    }

    public override void Visit(Field field, FloatType type, ref float value)
    {
        value = (float)BitConverter.UInt64BitsToDouble(streams[_index++].ReadNext());
    }

    public override void Visit(Field field, Int8Type type, ref sbyte value)
    {
        value = unchecked((sbyte)streams[_index++].ReadNext());
    }

    public override void Visit(Field field, Int16Type type, ref short value)
    {
        value = unchecked((short)streams[_index++].ReadNext());
    }

    public override void Visit(Field field, Int32Type type, ref int value)
    {
        value = unchecked((int)streams[_index++].ReadNext());
    }

    public override void Visit(Field field, Int64Type type, ref long value)
    {
        value = unchecked((long)streams[_index++].ReadNext());
    }

    public override void Visit(Field field, UInt8Type type, ref byte value)
    {
        value = (byte)streams[_index++].ReadNext();
    }

    public override void Visit(Field field, UInt16Type type, ref ushort value)
    {
        value = (ushort)streams[_index++].ReadNext();
    }

    public override void Visit(Field field, UInt32Type type, ref uint value)
    {
        value = (uint)streams[_index++].ReadNext();
    }

    public override void Visit(Field field, UInt64Type type, ref ulong value)
    {
        value = streams[_index++].ReadNext();
    }

    public override void Visit(Field field, CharType type, ref char value)
    {
        value = (char)streams[_index++].ReadNext();
    }

    public override void Visit(Field field, BoolType type, ref bool value)
    {
        value = streams[_index++].ReadNext() != 0;
    }
}
