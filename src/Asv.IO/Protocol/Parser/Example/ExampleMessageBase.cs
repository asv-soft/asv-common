using System;

namespace Asv.IO;

public abstract class ExampleMessageBase : IProtocolMessage<byte>
{
    public static byte CalcCrc(ReadOnlySpan<byte> buff)
    {
        byte crc = 0;
        foreach (var c in buff)
        {
            if (crc == 0)
            {
                crc = c;
            }
            else
            {
                crc ^= c;
            }
        }
        return crc;
    }
    
    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    { 
        if (buffer[0] != ExampleParser.SyncByte)
        {
            throw new ProtocolDeserializeMessageException(Protocol, this, "Invalid sync byte");
        }
        if (buffer[1] != Id)
        {
            throw new ProtocolDeserializeMessageException(Protocol, this, $"Invalid message id: want {Id}, got {buffer[1]}");
        }
        if (buffer.Length < 4)
        {
            throw new ProtocolDeserializeMessageException(Protocol, this, $"Message too short");
        }
        var calcCrc = CalcCrc(buffer[1..^1]);
        if (calcCrc != buffer[^1])
        {
            throw new ProtocolDeserializeMessageException(Protocol, this, $"Invalid crc: want {calcCrc}, got {buffer[^1]}");
        }
        var size = buffer[2];
        var internalBuffer = buffer[3..^1];
        InternalDeserialize(ref internalBuffer);
        buffer = buffer[(4 + size)..];
    }
    
    public void Serialize(ref Span<byte> buffer)
    {
        var origin = buffer;
        BinSerialize.WriteByte(ref buffer, ExampleParser.SyncByte);
        BinSerialize.WriteByte(ref buffer, Id);
        var sizeSpan = buffer;
        buffer = buffer[1..];
        var payload = buffer;
        InternalSerialize(ref buffer);
        var size = (byte)(payload.Length - buffer.Length);
        BinSerialize.WriteByte(ref sizeSpan, size);
        BinSerialize.WriteByte(ref buffer, CalcCrc(origin[1..^1]));
    }
    protected abstract void InternalDeserialize(ref ReadOnlySpan<byte> buffer);
    protected abstract void InternalSerialize(ref Span<byte> buffer);
    protected abstract int InternalGetByteSize();
    
    public virtual int GetByteSize() => 3 /*SYNC + ID + SIZE + CRC*/ + InternalGetByteSize();
    
    public ProtocolInfo Protocol => ExampleProtocol.Info;
    public ProtocolTags Tags { get; } = new();
    public abstract string Name { get; }
    public string GetIdAsString() => Id.ToString();
    public abstract byte Id { get; }
}