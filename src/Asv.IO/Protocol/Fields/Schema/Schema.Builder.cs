using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Asv.IO;

public partial class Schema
{
    public class Builder
    {
        private readonly ImmutableArray<Field>.Builder _fields = ImmutableArray.CreateBuilder<Field>();
        private ImmutableDictionary<string,string>.Builder? _metadata = null;

        public Builder Clear()
        {
            _fields.Clear();
            _metadata?.Clear();
            return this;
        }
        public Builder Field(Field field)
        {
            _fields.Add(field);
            return this;
        }
        
        public Builder Field(Action<Field.Builder> fieldBuilderAction)
        {
            var fieldBuilder = new Field.Builder();
            fieldBuilderAction(fieldBuilder);
            Field field = fieldBuilder.Build();

            _fields.Add(field);
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
        public Schema Build()
        {
            return new Schema(_fields.ToImmutable(), _metadata?.ToImmutable() ?? ImmutableDictionary<string, string>.Empty);
        }
        
        public override string ToString() => $"{nameof(Schema)}: Num fields={_fields.Count}, Num metadata={_metadata?.Count ?? 0}";
        
    }
}