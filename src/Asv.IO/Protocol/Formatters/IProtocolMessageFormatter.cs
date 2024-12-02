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

public interface IProtocolMessageFormatterBuilder
{
    void Clear();
    void Register(IProtocolMessageFormatter formatter);
}

public static class ProtocolMessageFormatterHelper
{
    public static void RegisterJsonFormatter(this IProtocolMessageFormatterBuilder builder)
    {
        builder.Register(new JsonMessageFormatter());
    }
    public static void RegisterSimpleFormatter(this IProtocolMessageFormatterBuilder builder)
    {
        builder.Register(new SimpleMessageToStringFormatter());
    }
}