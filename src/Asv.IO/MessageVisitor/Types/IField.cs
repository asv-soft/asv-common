using System.Collections.Immutable;

namespace Asv.IO.MessageVisitor;

public interface IField
{
    ImmutableDictionary<string, object?> Metadata { get; }
    string Name { get; }
    IType FieldType { get; }
}