using System;

namespace Asv.IO;

public interface IProtocolMessage:ISizedSpanSerializable, ISupportTag
{
    /// <summary>
    /// Gets the unique identifier of the protocol.
    /// </summary>
    /// <remarks>
    /// The Info property is a string that represents the unique identifier
    /// assigned to the protocol.
    /// </remarks>
    ProtocolInfo Protocol { get; }
    string Name { get; }
    string GetIdAsString();
}

public interface IProtocolMessage<out TMessageId> : IProtocolMessage
{
    TMessageId Id { get; }
}