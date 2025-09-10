namespace Asv.IO;

public class SimpleMessageToStringFormatter : IProtocolMessageFormatter
{
    public const string PrinterName = "Simple message.ToString() formatter";

    public string Name => PrinterName;
    public int Order => int.MaxValue;

    public bool CanPrint(IProtocolMessage message)
    {
        return true;
    }

    public string Print(IProtocolMessage packet, PacketFormatting formatting)
    {
        return packet.ToString() ?? string.Empty;
    }
}
