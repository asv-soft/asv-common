namespace Asv.IO;

public class ChimpFieldCounterVisitor : ChimpVisitorBase
{
    public int Count { get; private set; }

    public override void Visit(Field field, DoubleType type, ref double value) => ++Count;

    public override void Visit(Field field, FloatType type, ref float value) => ++Count;

    public override void Visit(Field field, Int8Type type, ref sbyte value) => ++Count;

    public override void Visit(Field field, Int16Type type, ref short value) => ++Count;

    public override void Visit(Field field, Int32Type type, ref int value) => ++Count;

    public override void Visit(Field field, Int64Type type, ref long value) => ++Count;

    public override void Visit(Field field, UInt8Type type, ref byte value) => ++Count;

    public override void Visit(Field field, UInt16Type type, ref ushort value) => ++Count;

    public override void Visit(Field field, UInt32Type type, ref uint value) => ++Count;

    public override void Visit(Field field, UInt64Type type, ref ulong value) => ++Count;

    public override void Visit(Field field, CharType type, ref char value) => ++Count;

    public override void Visit(Field field, BoolType type, ref bool value) => ++Count;
}
