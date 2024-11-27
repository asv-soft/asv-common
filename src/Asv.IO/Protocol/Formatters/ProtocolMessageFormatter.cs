using System.Diagnostics;

namespace Asv.IO;

public abstract class ProtocolMessageFormatter<TMessage> : IProtocolMessageFormatter
    where TMessage:IProtocolMessage
{
    public abstract string Name { get; }
    public abstract int Order { get; }
    public bool CanPrint(IProtocolMessage message)
    {
        return message is TMessage;
    }

    public string Print(IProtocolMessage packet, PacketFormatting formatting)
    {
        Debug.Assert(packet is TMessage);
        return Print((TMessage) packet, formatting);
    }
    
    protected abstract string Print(TMessage packet, PacketFormatting formatting);
}