using System;
using System.Collections.Immutable;

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
}

public readonly struct FieldVisitor(Action<Field> callback) : IVisitor
{
    public void VisitUnknown(Field field) => callback(field);
}