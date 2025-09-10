using System;
using Newtonsoft.Json;

namespace Asv.IO;

public class JsonMessageFormatter : IProtocolMessageFormatter
{
    public const string PrinterName = "JSON formatter";

    public string Name => PrinterName;
    public int Order => int.MaxValue;

    public bool CanPrint(IProtocolMessage message)
    {
        return true;
    }

    public string Print(IProtocolMessage packet, PacketFormatting formatting)
    {
        return formatting switch
        {
            PacketFormatting.Inline => JsonConvert.SerializeObject(packet, Formatting.None),
            PacketFormatting.Indented => JsonConvert.SerializeObject(packet, Formatting.Indented),
            _ => throw new ArgumentException("Wrong packet formatting!"),
        };
    }
}
