using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Asv.IO;

public static class FieldVisitorMixin
{
    public static void PrintFields(this IVisitable visitable, StringBuilder builder)
    {
        var visitor = new FieldVisitor(
            (f, t) =>
            {
                builder.Append(string.Join(".", f)).Append('[').Append(t.Name).Append(']');
            }
        );
        visitable.Accept(visitor);
    }

    public static void VisitFields(
        this IVisitable visitable,
        Action<Stack<Field>, IFieldType> callback
    )
    {
        var visitor = new FieldVisitor(callback);
        visitable.Accept(visitor);
    }

    public static void PrintValues(
        this IVisitable visitable,
        StringBuilder sb,
        bool skipUnknown = false
    )
    {
        var printVisitor = new PrintValueVisitor(sb, skipUnknown);
        visitable.Accept(printVisitor);
    }

    public static string PrintValues(this IVisitable visitable, bool skipUnknown = false)
    {
        var sb = new StringBuilder();
        var printVisitor = new PrintValueVisitor(sb, skipUnknown);
        visitable.Accept(printVisitor);
        return sb.ToString();
    }
}

public struct FieldVisitor(Action<Stack<Field>, IFieldType> callback)
    : StructType.IVisitor,
        ListType.IVisitor,
        ArrayType.IVisitor
{
    private readonly Stack<Field> _path = new();
    private bool _ignoreUnknown = false;

    public void VisitUnknown(Field field, IFieldType type)
    {
        if (_ignoreUnknown)
        {
            return;
        }

        _path.Push(field);
        callback(_path, type);
        _path.Pop();
    }

    public void BeginStruct(Field field, StructType type)
    {
        _path.Push(field);
        callback(_path, type);
    }

    public void EndStruct()
    {
        if (_path.Count == 0)
        {
            throw new InvalidOperationException("EndStruct called without matching BeginStruct");
        }

        _path.Pop();
    }

    public void BeginList(Field field, ListType type, ref uint size)
    {
        _path.Push(field);
        callback(_path, type);
        _ignoreUnknown = true;
    }

    public void EndList()
    {
        if (_path.Count == 0)
        {
            throw new InvalidOperationException("EndList called without matching BeginList");
        }

        _path.Pop();
        _ignoreUnknown = false;
    }

    public void BeginArray(Field field, ArrayType fieldType)
    {
        _path.Push(field);
        callback(_path, fieldType);
        _ignoreUnknown = true;
    }

    public void EndArray()
    {
        if (_path.Count == 0)
        {
            throw new InvalidOperationException("EndArray called without matching BeginArray");
        }

        _path.Pop();
        _ignoreUnknown = false;
    }
}
