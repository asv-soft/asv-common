namespace Asv.IO;

public interface IMessageFormatter
{
    string? PrintMessage(IProtocolMessage message, PacketFormatting formatting = PacketFormatting.Inline);
}