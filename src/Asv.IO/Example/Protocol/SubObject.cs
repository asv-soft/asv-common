using System;

namespace Asv.IO;

public record SubObject : IVisitable, ISizedSpanSerializable
{
    private static StructType? _type;
    public static StructType StructType =>
        _type ??= new StructType([Field1Field, Field2Field, Field3Field, Field4Field]);

    public static readonly Field Field1Field = new Field.Builder()
        .Name(nameof(Field1))
        .DataType(Int8Type.Default)
        .Title("Title  message  field 1")
        .Description("Description message field 1")
        .Build();

    private sbyte _field1;
    public sbyte Field1
    {
        get => _field1;
        set => _field1 = value;
    }

    public static readonly Field Field2Field = new Field.Builder()
        .Name(nameof(Field2))
        .DataType(UInt8Type.Default)
        .Title("Title  message  field 2")
        .Description("Description message field 2")
        .Build();
    private byte _field2;
    public byte Field2
    {
        get => _field2;
        set => _field2 = value;
    }

    public static readonly Field Field3Field = new Field.Builder()
        .Name(nameof(Field3))
        .DataType(Int16Type.Default)
        .Title("Title  message  field 3")
        .Description("Description message field 3")
        .Build();
    private short _field3;
    public short Field3
    {
        get => _field3;
        set => _field3 = value;
    }

    public static readonly Field Field4Field = new Field.Builder()
        .Name(nameof(Field4))
        .DataType(UInt16Type.Default)
        .Title("Title  message  field 4")
        .Description("Description message field 4")
        .Build();
    private ushort _field4;
    public ushort Field4
    {
        get => _field4;
        set => _field4 = value;
    }

    public void Accept(IVisitor visitor)
    {
        Int8Type.Accept(visitor, Field1Field, Field1Field.DataType, ref _field1);
        UInt8Type.Accept(visitor, Field2Field, Field2Field.DataType, ref _field2);
        Int16Type.Accept(visitor, Field3Field, Field3Field.DataType, ref _field3);
        UInt16Type.Accept(visitor, Field4Field, Field4Field.DataType, ref _field4);
    }

    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        BinSerialize.ReadSByte(ref buffer, ref _field1);
        BinSerialize.ReadByte(ref buffer, ref _field2);
        BinSerialize.ReadShort(ref buffer, ref _field3);
        BinSerialize.ReadUShort(ref buffer, ref _field4);
    }

    public void Serialize(ref Span<byte> buffer)
    {
        BinSerialize.WriteSByte(ref buffer, _field1);
        BinSerialize.WriteByte(ref buffer, _field2);
        BinSerialize.WriteShort(ref buffer, _field3);
        BinSerialize.WriteUShort(ref buffer, _field4);
    }

    public int GetByteSize()
    {
        return sizeof(sbyte) /*Field1*/
            + sizeof(byte) /*Field2*/
            + sizeof(short) /*Field3*/
            + sizeof(ushort) /*Field4*/
        ;
    }
}
