using System;
using System.Collections.Immutable;
using System.Text;

namespace Asv.IO;

public static class FieldVisitorMixin
{
    public static ImmutableArray<Field> GetFields(this IVisitable visitable)
    {
        var fields = ImmutableArray.CreateBuilder<Field>();
        var visitor = new FieldVisitor(v=>fields.Add(v));
        visitable.Accept(visitor);
        return fields.ToImmutable();
    }

    public static void PrintValues(this IVisitable visitable, StringBuilder sb)
    {
        var printVisitor = new PrintValueVisitor(sb);
        visitable.Accept(printVisitor);
    }
    
    public static string PrintValues(this IVisitable visitable)
    {
        var sb = new StringBuilder();
        var printVisitor = new PrintValueVisitor(sb);
        visitable.Accept(printVisitor);
        return sb.ToString();
    }
    
}

public readonly struct FieldVisitor(Action<Field> callback) : IVisitor
{
    public void VisitUnknown(Field field) => callback(field);
}

public struct PrintValueVisitor(StringBuilder sb) : IFullVisitor
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
    public void Visit(Field field, ref byte value)
    {
        CheckFirst();
        sb.Append(value);
    }

    

    public void Visit(Field field, ref sbyte value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public void Visit(Field field, ref short value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public void Visit(Field field, ref ushort value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public void Visit(Field field, ref int value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public void Visit(Field field, ref uint value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public void Visit(Field field, ref long value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public void Visit(Field field, ref ulong value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public void Visit(Field field, ref float value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public void Visit(Field field, ref double value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public void Visit(Field field, ref string value)
    {
        CheckFirst();
        sb.Append('\'');
        sb.Append(value);
        sb.Append('\'');
    }

    public void Visit(Field field, ref bool value)
    {
        CheckFirst();
        sb.Append(value);
    }

    public void VisitUnknown(Field field)
    {
        // This method is not implemented in this visitor
    }

    public void BeginArray(Field field, int size)
    {
        CheckFirst();
        _first = true;
        sb.Append('[');
    }

    public void EndArray()
    {
        _first = false;
        sb.Append(']');
    }

    public void BeginStruct(Field field)
    {
        CheckFirst();
        _first = true;
        sb.Append('{');
    }

    public void EndStruct()
    {
        _first = false;
        sb.Append('}');
    }

    public void BeginList(Field field, ref uint size)
    {
        CheckFirst();
        _first = true;
        sb.Append('<');
    }

    public void EndList()
    {
        _first = false;
        sb.Append('>');
    }
}

