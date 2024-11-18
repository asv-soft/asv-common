namespace Asv.IO;

public interface IProtocolMessage:ISizedSpanSerializable
{
    /// <summary>
    /// Gets the unique identifier of the protocol.
    /// </summary>
    /// <remarks>
    /// The ProtocolId property is a string that represents the unique identifier
    /// assigned to the protocol.
    /// </remarks>
    string ProtocolId { get; }
    ProtocolTags Tags { get; }
}