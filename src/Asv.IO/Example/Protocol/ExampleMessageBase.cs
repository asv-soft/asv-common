using System;

namespace Asv.IO;
/// <summary>
/// Example message base class
/// [SYNC:1][SENDER_ID:1][MSG_ID:1][PAYLOAD_SIZE:1][PAYLOAD:SIZE][CRC:1, RANGE=1..^1]
/// </summary>
public abstract class ExampleMessageBase : IProtocolMessage<byte>, IVisitable
{
    private ProtocolTags _tags;

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
        SenderId = buffer[1];
        if (buffer[2] != Id)
        {
            throw new ProtocolDeserializeMessageException(Protocol, this, $"Invalid message id: want {Id}, got {buffer[1]}");
        }
        if (buffer.Length < 5)
        {
            throw new ProtocolDeserializeMessageException(Protocol, this, $"Message too short");
        }
        var calcCrc = CalcCrc(buffer[1..^1]);
        if (calcCrc != buffer[^1])
        {
            throw new ProtocolDeserializeMessageException(Protocol, this, $"Invalid crc: want {calcCrc}, got {buffer[^1]}");
        }
        var size = buffer[3];
        var internalBuffer = buffer[4..^1];
        InternalDeserialize(ref internalBuffer);
        buffer = buffer[(5 + size)..];
    }
    
    public void Serialize(ref Span<byte> buffer)
    {
        var origin = buffer;
        BinSerialize.WriteByte(ref buffer, ExampleParser.SyncByte);
        BinSerialize.WriteByte(ref buffer, SenderId);
        BinSerialize.WriteByte(ref buffer, Id);
        var sizeRef = buffer;
        buffer = buffer[1..];
        var payload = buffer;
        InternalSerialize(ref buffer);
        var size = (byte)(payload.Length - buffer.Length);
        BinSerialize.WriteByte(ref sizeRef, size);
        var forCrc = origin[1..(size + 5)];
        BinSerialize.WriteByte(ref buffer, CalcCrc(forCrc));
    }
    protected abstract void InternalDeserialize(ref ReadOnlySpan<byte> buffer);
    protected abstract void InternalSerialize(ref Span<byte> buffer);
    protected abstract int InternalGetByteSize();
    
    public virtual int GetByteSize() => 5 /*SYNC + ID + SIZE + SENDER_ID + CRC*/ + InternalGetByteSize();
    
    public ProtocolInfo Protocol => ExampleProtocol.Info;
    
    public abstract string Name { get; }
    public string GetIdAsString() => Id.ToString();
    public abstract byte Id { get; }
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public ref ProtocolTags Tags => ref _tags;
    public byte SenderId { get; set; }
    
    public abstract void Accept(IVisitor visitor);
}