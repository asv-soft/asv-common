using System;
using System.Collections.Immutable;

namespace Asv.IO.MessageVisitor;

public abstract class FieldBuilder<TSelf, TField> : IFieldBuilder<TSelf, TField> 
    where TSelf : IFieldBuilder<TSelf, TField> 
    where TField : IField
{
    private string? _name;

    public ImmutableDictionary<string, object?>.Builder Metadata { get; } = ImmutableDictionary.CreateBuilder<string, object?>();
    
    public abstract TSelf MySelf { get; }
    protected abstract TField Build(string name, ImmutableDictionary<string, object?> metadata);

    public TSelf Name(string name)
    {
        _name = name;
        return MySelf;
    }

    public TField Build()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(_name);
        return Build(_name, Metadata.ToImmutable());
    }

}