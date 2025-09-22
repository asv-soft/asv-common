using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Asv.IO;

public class ChimpColumnVisitor : ChimpVisitorBase
{
    private readonly Stack<string> _path = new();
    private bool _isArray;
    private int _arrayIndex;

    public List<string> Columns { get; } = [];
    public int Count => Columns.Count;

    public override void Visit(Field field, DoubleType type, ref double value) =>
        AcceptSimpleType(field.Name);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AcceptSimpleType(string fieldName)
    {
        Columns.Add(
            _path.Count == 0
                ? fieldName
                : string.Join('.', _path.Reverse())
                    + (_isArray ? $"[{_arrayIndex++}]" : "." + fieldName)
        );
    }

    public override void Visit(Field field, FloatType type, ref float value) =>
        AcceptSimpleType(field.Name);

    public override void Visit(Field field, Int8Type type, ref sbyte value) =>
        AcceptSimpleType(field.Name);

    public override void Visit(Field field, Int16Type type, ref short value) =>
        AcceptSimpleType(field.Name);

    public override void Visit(Field field, Int32Type type, ref int value) =>
        AcceptSimpleType(field.Name);

    public override void Visit(Field field, Int64Type type, ref long value) =>
        AcceptSimpleType(field.Name);

    public override void Visit(Field field, UInt8Type type, ref byte value) =>
        AcceptSimpleType(field.Name);

    public override void Visit(Field field, UInt16Type type, ref ushort value) =>
        AcceptSimpleType(field.Name);

    public override void Visit(Field field, UInt32Type type, ref uint value) =>
        AcceptSimpleType(field.Name);

    public override void Visit(Field field, UInt64Type type, ref ulong value) =>
        AcceptSimpleType(field.Name);

    public override void Visit(Field field, CharType type, ref char value) =>
        AcceptSimpleType(field.Name);

    public override void Visit(Field field, BoolType type, ref bool value) =>
        AcceptSimpleType(field.Name);

    public override void BeginArray(Field field, ArrayType fieldType)
    {
        _isArray = true;
        _path.Push(field.Name);
        base.BeginArray(field, fieldType);
    }

    public override void EndArray()
    {
        _path.Pop();
        _isArray = false;
        base.EndArray();
    }

    public override void BeginStruct(Field field, StructType type)
    {
        _path.Push(field.Name);
        base.BeginStruct(field, type);
    }

    public override void EndStruct()
    {
        _path.Pop();
        base.EndStruct();
    }
}
