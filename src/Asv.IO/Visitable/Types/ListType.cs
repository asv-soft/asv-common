using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Asv.IO;

public sealed class ListType(Field valueField) : NestedType([valueField])
{
    public ListType(FieldType valueDataType)
        : this(new Field("item", valueDataType, ImmutableDictionary<string, string>.Empty)) { }
    public override FieldTypeId TypeId => FieldTypeId.List;
    public override string Name => "list";

    public Field ValueField => Fields[0];

    public IFieldType ValueDataType => Fields[0].DataType;
    
    public static void Accept<T>(IVisitor visitor, Field field, IList<T> list, Action<int,IVisitor> callback) 
        where T : new()
    {
        if (visitor is IListVisitor accept)
        {
            var newSize = list.Count;
            accept.BeginList(field, ref newSize);
            while (newSize > list.Count)
            {
                list.Add(new T());
            }
            while (newSize < list.Count)
            {
                list.RemoveAt(list.Count - 1);
            }
            for (var i = 0; i < newSize; i++)
            {
                callback(i, visitor);
            }
            accept.EndArray();
        }
        else
        {
            visitor.VisitUnknown(field);
        }
    }

}

public interface IListVisitor: IVisitor
{
    void BeginList(Field field, ref int size);
    void EndArray();
}