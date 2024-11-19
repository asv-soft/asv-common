using System;
using System.Collections.Immutable;
using System.Collections.Specialized;

namespace Asv.IO;


//          userinfo       host      port
//          ┌──┴───┐ ┌──────┴──────┐ ┌┴─┐
//  tcp_s://john.doe@www.example.com:1234/forum/questions/?br=115200&timeout=1000#protocol=mavlink&feature
//  └─┬─┘   └─────────────┬─────────────┘└───────┬───────┘ └────────────┬────────────┘ └───────┬───────┘
//  scheme            authority                path                   query                 fragment

public interface IProtocol
{
   
}

public class Protocol:IProtocol
{
    #region Static

    public static IProtocol Create(Action<IProtocolBuilder> configure)
    {
        var builder = new ProtocolBuilder();
        configure(builder);
        return builder.Build();
    }
    
    private static NameValueCollection ParseQueryString(string requestQueryString)
    {
        var rc = new NameValueCollection();
        var ar1 = requestQueryString.Split('&', '?');
        foreach (var row in ar1)
        {
            if (string.IsNullOrEmpty(row)) continue;
            var index = row.IndexOf('=');
            if (index < 0) continue;
            rc[Uri.UnescapeDataString(row[..index])] = Uri.UnescapeDataString(row[(index + 1)..]); // use Unescape only parts          
        }
        return rc;
    }

    #endregion
    
    private readonly ImmutableArray<IProtocolProcessingFeature> _features;
    private readonly ImmutableDictionary<string, ParserFactoryDelegate> _parsers;
    private readonly ImmutableArray<ProtocolInfo> _protocols;
    private readonly ImmutableDictionary<string, PortFactoryDelegate> _ports;
    private readonly ImmutableArray<PortTypeInfo> _portInfos;
    private readonly IProtocolCore _core;
    
    internal Protocol(
        ImmutableArray<IProtocolProcessingFeature> features, 
        ImmutableDictionary<string, ParserFactoryDelegate> parsers, 
        ImmutableArray<ProtocolInfo> protocols, 
        ImmutableDictionary<string,PortFactoryDelegate> ports, 
        ImmutableArray<PortTypeInfo> portInfos,
        IProtocolCore core)
    {
        _features = features;
        _parsers = parsers;
        _protocols = protocols;
        _ports = ports;
        _portInfos = portInfos;
        _core = core;
    }
}