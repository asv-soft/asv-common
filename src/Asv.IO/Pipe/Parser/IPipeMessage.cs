using System;

namespace Asv.IO;

public interface IPipeMessage
{
    /// <summary>
    /// Gets the unique identifier of the protocol.
    /// </summary>
    /// <remarks>
    /// The ProtocolId property is a string that represents the unique identifier
    /// assigned to the protocol.
    /// </remarks>
    string ProtocolId { get; }
}