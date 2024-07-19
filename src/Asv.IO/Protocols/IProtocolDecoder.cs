using System;
using Asv.Common;

namespace Asv.IO;

/// <summary>
/// Interface for message decoders.
/// </summary>
public interface IProtocolDecoder: IDisposable
{
    /// <summary>
    /// Gets the identifier of the protocol.
    /// </summary>
    /// <remarks>
    /// The ProtocolId property represents the unique identifier of the protocol.
    /// This identifier is used to distinguish between different protocols.
    /// </remarks>
    /// <value>
    /// A string representing the protocol identifier.
    /// </value>
    string ProtocolId { get; }
    /// <summary>
    /// Reads the given byte and returns a boolean value.
    /// </summary>
    /// <param name="data">The byte to be read.</param>
    /// <returns>A boolean value indicating whether the message was founded.</returns>
    bool Read(byte data);
    /// <summary>
    /// Resets the state of the decoder.
    /// It may be necessary to reset the decoder if we are using multiple decoders in the same stream:
    /// if another decoder has found a message, we need to reset all other decoder to avoid errors.
    /// </summary>
    void Reset();
    /// <summary>
    /// Output error messages.
    /// </summary>
    IObservable<MessageDecoderException> OnError { get; }
    IObservable<IProtocolMessage> OnRxMessage { get; }
}

public class ProtocolDecoderBase : DisposableOnce, IProtocolDecoder
{
    public ProtocolDecoderBase(string protocolId)
    {
        if (string.IsNullOrWhiteSpace(protocolId))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(protocolId));
        ProtocolId = protocolId;
    }

    protected override void InternalDisposeOnce()
    {
        throw new NotImplementedException();
    }

    public IDisposable Subscribe(IObserver<IProtocolMessage> observer)
    {
        throw new NotImplementedException();
    }

    public string ProtocolId { get; }
    
    public bool Read(byte data)
    {
        throw new NotImplementedException();
    }

    public void Reset()
    {
        throw new NotImplementedException();
    }

    public IObservable<MessageDecoderException> OnError { get; }
    public IObservable<IProtocolMessage> OnRxMessage { get; }
}