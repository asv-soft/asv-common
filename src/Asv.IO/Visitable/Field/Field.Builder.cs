using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Asv.IO;


public partial class Field
{
    
    
    public class Builder
    {
        private string? _name;
        private ImmutableDictionary<string, string>.Builder? _metadata;
        private IFieldType? _type;

        public virtual Field Build()
        {
            return new Field(_name ?? throw new InvalidOperationException(), _type ?? throw new InvalidOperationException(), _metadata?.ToImmutable() ?? ImmutableDictionary<string, string>.Empty);
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
        
        public Builder Metadata(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            _metadata ??= ImmutableDictionary.CreateBuilder<string, string>();

            _metadata[key] = value;
            return this;
        }
        
        public Builder Metadata(IEnumerable<KeyValuePair<string, string>> dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            foreach (var entry in dictionary)
            {
                Metadata(entry.Key, entry.Value);
            }
            return this;
        }


    }
}