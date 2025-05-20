using System;
using System.Collections.Immutable;

namespace Asv.IO;

public sealed class Int8Type : IntegerType
{
    public static readonly Int8Type Default = new();

    
    public override FieldTypeId TypeId => FieldTypeId.Int8;
    public override string Name => "int8";
    public override int BitWidth => 8;
    public override int ByteWidth => 1;
    public override bool IsSigned => true;
    public static void Accept(IVisitor visitor,Field field, ref sbyte value) => FieldType.Accept(visitor, field, ref value);
    
}

public class Example
{
    public Field field = new TInt8.Field.Builder()
        .Name("asdasd")
        .Max(10)
        .Build();
}

public sealed class TInt8 : IntegerType
{
    public const string TypeName = "int8";
    
    public static readonly Int8Type Instance = new();
    public override FieldTypeId TypeId => FieldTypeId.Int8;
    public override string Name => TypeName;
    public override int BitWidth => 8;
    public override int ByteWidth => 1;
    public override bool IsSigned => true;

    public interface IVisitor : IVisitor<sbyte>
    {
        
    }
    
    public static void Accept(Asv.IO.IVisitor visitor, Field field, ref sbyte value)
    {
        if (visitor is IVisitor accept)
        {
            accept.Visit(field, ref value);
        }
        else
        {
            visitor.VisitUnknown(field);
        }
    }
    
    public class Field(string name, ImmutableDictionary<string, string> metadata) : Asv.IO.Field(name, Instance, metadata)
    {
        public new class Builder 
        {
            private string _name;
            private readonly IO.Field.Builder _base;

            public Builder()
            {
                _base = new Asv.IO.Field.Builder();
            }

            public Builder Name(string name)
            {
                _base.Name(name);
                return this;
            }
            
            public Field Build()
            {
                return null;
            }


            public Builder Max(sbyte max)
            {
                throw new NotImplementedException();
            }
        }
    }
    
}

