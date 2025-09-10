using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Asv.IO;

public partial class Field
{
    public static Builder CreateBuilder() => new();

    public sealed class Builder
    {
        private string? _name;
        private readonly ImmutableDictionary<string, object?>.Builder _metadata =
            ImmutableDictionary.CreateBuilder<string, object?>();
        private IFieldType? _type;

        public Field Build()
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(_name);
            ArgumentNullException.ThrowIfNull(_type);
            return new Field(_name, _type, _metadata.ToImmutable());
        }

        public Builder Name(string value)
        {
            _name = value ?? throw new ArgumentNullException(nameof(value));
            return this;
        }

        public Builder DataType(IFieldType type)
        {
            _type = type;
            return this;
        }

        public Builder Metadata(string key, object? value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }
            _metadata[key] = value;
            return this;
        }
    }
}
