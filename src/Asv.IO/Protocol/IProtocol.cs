using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Asv.IO;


//          userinfo       host      port
//          ┌──┴───┐ ┌──────┴──────┐ ┌┴─┐
//  tcp_s://john.doe@www.example.com:1234/forum/questions/?br=115200&timeout=1000#protocol=mavlink&feature
//  └─┬─┘   └─────────────┬─────────────┘└───────┬───────┘ └────────────┬────────────┘ └───────┬───────┘
//  scheme            authority                path                   query                 fragment

public interface IProtocol
{
    IProtocolCore Core { get; }
    ImmutableArray<IProtocolFeature> Features { get; }
    IEnumerable<PortTypeInfo> Ports { get; }
    IEnumerable<ProtocolInfo> Protocols { get; }
    IProtocolPort CreatePort(Uri connectionString);
    IProtocolParser CreateParser(string protocolId);
    IProtocolRouter CreateRouter(string id);
    string? PrintMessage(IProtocolMessage message, PacketFormatting formatting = PacketFormatting.Inline);
}

public enum PacketFormatting
{
    Inline,
    Indented,
}

public static partial class ProtocolHelper
{
    public static IProtocolPort CreatePort(this IProtocol src,string connectionString)
    {
        return src.CreatePort(new Uri(connectionString));
    }

    internal static string NormalizeId(string id)
    {
        return _regex.Replace(id, "_");
    }
    [GeneratedRegex(@"[^\w]")]
    private static partial Regex MyRegex();
    private static Regex _regex = MyRegex();
}