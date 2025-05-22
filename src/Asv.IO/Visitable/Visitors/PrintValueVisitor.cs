using System;
using System.Text;

namespace Asv.IO;

public class PrintValueVisitor(StringBuilder sb, bool skipUnknown) : FullVisitorBase(skipUnknown)
{
    private bool _first = true;

    private void CheckFirst()
    {
        if (_first)
        {
            _first = false;
        }
        else
        {
            sb.Append(", ");
        }
    }
    public override void Visit(Field field, UInt8Type type, ref byte value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public override void Visit(Field field, HalfFloatType type, ref Half value)
    {
        CheckFirst();
        sb.Append(value);
    }


    public override void Visit(Field field, Int8Type type, ref sbyte value)
    {
        CheckFirst();
        sb.Append(value);
    }
    public override void Visit(Field field, Int16Type type, ref short value)
    {
        CheckFirst();
        sb.Append(value);
    }
    public override void Visit(Field field, UInt16Type type, ref ushort value)
    {
        CheckFirst();
        sb.Append(value);
    }
    public override void Visit(Field field, Int32Type type, ref int value)
    {
        CheckFirst();
        sb.Append(value);
    }
    public override void Visit(Field field, UInt32Type type, ref uint value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public override void Visit(Field field, Int64Type type, ref long value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public override void Visit(Field field, UInt64Type type, ref ulong value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public override void Visit(Field field, DoubleType type, ref double value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public override void Visit(Field field, FloatType type, ref float value)
    {
        CheckFirst();
        sb.Append(value);
    }

    

    public override void Visit(Field field, StringType type, ref string value)
    {
        CheckFirst();
        sb.Append('\'');
        sb.Append(value);
        sb.Append('\'');
    }

    public override void Visit(Field field, BoolType type, ref bool value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public override void Visit(Field field, CharType type, ref char value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public override void VisitUnknown(Field field, IFieldType type)
    {
        // This method is not implemented in this visitor
    }

    public override void BeginArray(Field field, ArrayType fieldType, int size)
    {
        CheckFirst();
        _first = true;
        sb.Append('[');
    }

    public override void EndArray()
    {
        _first = false;
        sb.Append(']');
    }

    public override void BeginStruct(Field field, StructType type)
    {
        CheckFirst();
        _first = true;
        sb.Append('{');
    }

    public override void EndStruct()
    {
        _first = false;
        sb.Append('}');
    }

    public override void BeginList(Field field, ListType type, ref uint size)
    {
        CheckFirst();
        _first = true;
        sb.Append('<');
    }

    
    public override void EndList()
    {
        _first = false;
        sb.Append('>');
    }
}