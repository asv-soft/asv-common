using System;
using System.Collections.Immutable;

namespace Asv.IO.MessageVisitor;

public static class ArrayT
{
    public const string TypeId = "array";
    
    public class Type(int length) : IType
    {
        public string Id => TypeId;
        public int Length => length;
    }
    
    public interface IVisitor : IMessageVisitor
    {
        void BeginArray(Field field, int size);
        void EndArray();
    }

    public static void Accept(IMessageVisitor visitor, Field field, IType type, int size, Action<int,Field,IType,IMessageVisitor> callback)
    {
        if (visitor is IVisitor accept)
        {
            accept.BeginArray(field,size);
            for (var i = 0; i < size; i++)
            {
                callback(i, field, field.FieldType, visitor);
            }
            accept.EndArray();
        }
        else
        {
            visitor.VisitUnknown(field);
        }
    }
    
    public class Field(IType type, string name, ImmutableDictionary<string, object?> metadata)
        : FieldBase(type, name, metadata)
    {
        
    }
    
    public class Builder : FieldBuilder<Builder, Field>
    {
        public override Builder MySelf => this;

        protected override Field Build(string name, ImmutableDictionary<string, object?> metadata)
        {
            return null;
        }


        
    }
}

public static class Int8T
{
    public const string TypeId = "int8";
    public const string MetaDataMaxKey = "max";
    public const string MetaDataMinKey = "min";

    public class Type : IType
    {
        public static Type Default { get; } = new();
        public string Id => TypeId;
    }

    public interface IVisitor : IMessageVisitor<sbyte>
    {

    }
    
    public static void Accept(IMessageVisitor visitor, IField field, IType type, ref sbyte value)
    {
        if (visitor is IVisitor accept)
        {
            accept.Visit(field, type, ref value);
        }
        else
        {
            visitor.VisitUnknown(field);
        }
    }

    public class Field(IType type, string name, ImmutableDictionary<string, object?> metadata)
        : FieldBase(type, name, metadata)
    {
        public sbyte Max => Metadata.TryGetValue(MetaDataMaxKey, out var value)
            ? (sbyte)(value ?? sbyte.MaxValue)
            : sbyte.MaxValue;

        public sbyte Min => Metadata.TryGetValue(MetaDataMinKey, out var value)
            ? (sbyte)(value ?? sbyte.MinValue)
            : sbyte.MinValue;
    }

    public class Builder : FieldBuilder<Builder, Field>
    {
        public override Builder MySelf => this;

        protected override Field Build(string name, ImmutableDictionary<string, object?> metadata)
        {
            return new Field(Type.Default, name, metadata);
        }

        public Builder Max(sbyte value)
        {
            Metadata[MetaDataMaxKey] = value;
            return MySelf;
        }

        public Builder Min(sbyte value)
        {
            Metadata[MetaDataMinKey] = value;
            return MySelf;
        }

        
    }
}

