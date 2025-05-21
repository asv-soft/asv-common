using System.Collections.Immutable;

namespace Asv.IO.MessageVisitor;

public class FieldBase(IType fieldType, string name, ImmutableDictionary<string, object?> metadata)
    : IField
{
    public ImmutableDictionary<string, object?> Metadata { get; } = metadata;
    public string Name { get; } = name;
    public IType FieldType { get; } = fieldType;
}