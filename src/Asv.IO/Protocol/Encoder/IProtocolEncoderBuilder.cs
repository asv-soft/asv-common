namespace Asv.IO;

/// <summary>
/// Creates an independent encoder for one outbound endpoint and protocol.
/// </summary>
public delegate IProtocolEncoder EncoderFactoryDelegate(IProtocolContext context);

/// <summary>
/// Optional encoder registration capability for protocol parser builders.
/// </summary>
public interface IProtocolEncoderBuilder
{
    void Register(ProtocolInfo info, EncoderFactoryDelegate factory);
    void Clear();
}
