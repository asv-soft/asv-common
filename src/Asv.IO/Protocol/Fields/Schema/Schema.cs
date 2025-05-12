using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Asv.IO;

public partial class Schema(ImmutableArray<Field> fieldsList, ImmutableDictionary<string, string> metadata) 
    : IRecordType
{
    private readonly ImmutableDictionary<string,Field> _fieldDictionary = fieldsList.ToImmutableDictionary(x=>x.Name);
    public ImmutableDictionary<string, string> Metadata => metadata;
    public bool HasMetadata => Metadata.IsEmpty;
    public Field this[int index] => GetFieldByIndex(index);
    public Field this[string name] => GetFieldByName(name);
    public int FieldCount => fieldsList.Length;
    public Field GetFieldByIndex(int i) => fieldsList[i];
    public Field GetFieldByName(string name) => _fieldDictionary[name];
    public int GetFieldIndex(string name, StringComparer comparer)
    {
        IEqualityComparer<string> equalityComparer = comparer;
        return GetFieldIndex(name, equalityComparer);
    }
    public int GetFieldIndex(string name, IEqualityComparer<string>? comparer = null)
    {
        comparer ??= StringComparer.CurrentCulture;

        for (var i = 0; i < fieldsList.Length; i++)
        {
            if (comparer.Equals(fieldsList[i].Name, name))
                return i;
        }

        return -1;
    }

    public Schema RemoveField(int fieldIndex) => new(fieldsList.RemoveAt(fieldIndex), Metadata);

    public Schema InsertField(int fieldIndex, Field newField) => new(fieldsList.Add(newField), Metadata);

    public Schema SetField(int fieldIndex, Field newField) => new(fieldsList.SetItem(fieldIndex,newField), Metadata);

    public FieldTypeId TypeId => FieldTypeId.Schema;
    public string Name => nameof(Schema);

    public void Accept(IFieldTypeVisitor visitor)
    {
        if (visitor is IFieldTypeVisitor<Schema> schemaVisitor)
        {
            schemaVisitor.Visit(this);
        }
        else if (visitor is IFieldTypeVisitor<IRecordType> interfaceVisitor)
        {
            interfaceVisitor.Visit(this);
        }
        else
        {
            visitor.Visit(this);
        }
    }

    public bool IsFixedWidth => false;
}

