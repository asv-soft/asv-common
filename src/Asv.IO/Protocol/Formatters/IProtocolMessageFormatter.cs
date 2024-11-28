namespace Asv.IO;


public enum PacketFormatting
{
    Inline,
    Indented,
}

public interface IProtocolMessageFormatter
{
    string Name { get; }
    int Order { get; }
    bool CanPrint(IProtocolMessage message);
    string Print(IProtocolMessage packet, PacketFormatting formatting);
}

public static class ProtocolMessageFormatterHelper
{
    public static void AddPrinterJson(this IProtocolBuilder builder)
    {
        builder.RegisterFormatter(new JsonMessageFormatter());
    }
}