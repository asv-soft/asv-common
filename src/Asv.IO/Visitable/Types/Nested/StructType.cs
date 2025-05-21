using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Asv.IO;

public class StructType(ImmutableArray<Field> fields) : FieldType
{
    public const string TypeId = "struct";
    public override string Name => TypeId;
    
    private readonly ImmutableDictionary<string,Field> _fieldDict = fields.ToImmutableDictionary(x=>x.Name, x=>x);
    public ImmutableArray<Field> Fields => fields;
    public Field this[int index] => fields[index];
    public Field? this[string name] => _fieldDict[name];
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
    
    public static void Accept(Asv.IO.IVisitor visitor, Field field, IFieldType type, IVisitable value)
    {
        if (visitor is IVisitor accept)
        {
            var t = (StructType)type;
            accept.BeginStruct(field,t);
            value.Accept(visitor);
            accept.EndStruct();
        }
        else
        {
            visitor.VisitUnknown(field, type);
        }
    }
    
    public interface IVisitor: Asv.IO.IVisitor
    {
        void BeginStruct(Field field, StructType type);
        void EndStruct();
    }
}

