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