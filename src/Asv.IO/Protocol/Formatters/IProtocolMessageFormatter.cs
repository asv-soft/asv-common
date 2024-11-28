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
    public static void RegisterJsonFormatter(this IProtocolBuilder builder)
    {
        builder.RegisterFormatter(new JsonMessageFormatter());
    }
    public static void RegisterSimpleFormatter(this IProtocolBuilder builder)
    {
        builder.RegisterFormatter(new SimpleMessageToStringFormatter());
    }
}