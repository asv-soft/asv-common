using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Asv.IO;

public abstract class NestedType(ImmutableArray<Field> fields) : FieldType
{
    private readonly ImmutableDictionary<string,Field> _fieldDict = fields.ToImmutableDictionary(x=>x.Name, x=>x);
    public ImmutableArray<Field> Fields => fields;
    
    public Field this[int index] => GetFieldByIndex(index);
    public Field? this[string name] => GetFieldByName(name);
    public int FieldCount => fields.Length;
    public Field GetFieldByIndex(int i) => fields[i];
    public Field GetFieldByName(string name) => _fieldDict[name];
    public int GetFieldIndex(string name, StringComparer comparer)
    {
        IEqualityComparer<string> equalityComparer = comparer;
        return GetFieldIndex(name, equalityComparer);
    }
    public int GetFieldIndex(string name, IEqualityComparer<string>? comparer = null)
    {
        comparer ??= StringComparer.CurrentCulture;

        for (var i = 0; i < Fields.Length; i++)
        {
            if (comparer.Equals(Fields[i].Name, name))
                return i;
        }

        return -1;
    }
}