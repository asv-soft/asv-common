using System;

namespace Asv.IO;

/// <summary>
/// Encodes messages for one outbound protocol stream. Instances are not required to be thread-safe.
/// </summary>
/// <remarks>
/// Keep stream state in the encoder, independently of messages and receive parsers.
/// The owner must serialize encoding and sending in the same order. Create a new encoder for a
/// new stream; after a failed or partial write, do not reuse its state on another connection.
/// </remarks>
public interface IProtocolEncoder
{
    /// <summary>
    /// Returns a nonnegative upper bound for the next encoded message without changing state.
    /// </summary>
    int GetMaxByteSize(IProtocolMessage message);

    /// <summary>
    /// Writes one message and returns the actual number of bytes written to the start of the destination.
    /// </summary>
    /// <remarks>
    /// A destination of at least <see cref="GetMaxByteSize"/> bytes must be sufficient.
    /// Validate capacity and message data before committing stream state. On failure, preserve
    /// the previous encoder state; destination contents are unspecified. Do not mutate the message
    /// to store stream state. A successful call advances state exactly once, even when the encoded
    /// representation is shorter than the reserved capacity.
    /// </remarks>
    int Encode(IProtocolMessage message, Span<byte> destination);
}
