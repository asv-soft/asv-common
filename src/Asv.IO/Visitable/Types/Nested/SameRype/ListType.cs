using System;
using System.Collections.Generic;

namespace Asv.IO;

public sealed class ListType(IFieldType elementType, int minSize = 0, int maxSize = 100) : FieldType, INestedWithSameType
{
    public const string TypeId = "list";
    public override string Name => TypeId;
    public IFieldType ElementType => elementType;
    public int MaxSize => maxSize;
    public int MinSize => minSize;
    public static void Accept<TElement>(Asv.IO.IVisitor visitor, Field field, IFieldType type, IList<TElement> list, ElementDelegate callback) 
        where TElement : new()
    {
        if (visitor is IVisitor accept)
        {
            var t = (ListType)type;
            var newSize = (uint)list.Count;
            accept.BeginList(field, t, ref newSize);
            while (newSize > list.Count)
            {
                list.Add(new TElement());
            }
            while (newSize < list.Count)
            {
                list.RemoveAt(list.Count - 1);
            }
            for (var i = 0; i < newSize; i++)
            {
                callback(i, visitor, field, t.ElementType);
            }
            accept.EndList();
        }
        else
        {
            visitor.VisitUnknown(field, type);
        }
    }

    public interface IVisitor: Asv.IO.IVisitor
    {
        void BeginList(Field field, ListType type, ref uint size);
        void EndList();
    }
    
}

