using System;
using Newtonsoft.Json;

namespace Asv.IO;

public interface IProtocolMessage:ISizedSpanSerializable
{
    /// <summary>
    /// Protocol ID
    /// </summary>
    string ProtocolId { get; }
    /// <summary>
    /// Gets the readable name of the message.
    /// You must not use it for identification message type.
    /// </summary>
    string Name { get; }
    /// <summary>
    /// Gets or sets the custom use property (like routing, etc...)
    /// This field is not serialized or deserialized.
    /// </summary>
    /// <value>
    /// The custom use property.
    /// </value>
    object? Tag { get; set; }
}

public abstract class ProtocolMessageBase<TMsgId> : IProtocolMessage
{
    public abstract TMsgId MessageId { get; }
    public string? MessageIdAsString => MessageId?.ToString();
    public abstract void Deserialize(ref ReadOnlySpan<byte> buffer);
    public abstract void Serialize(ref Span<byte> buffer);
    public abstract int GetByteSize();
    
    public abstract string ProtocolId { get; }
    public abstract string Name { get; }
    [JsonIgnore]
    public object? Tag { get; set; }
}