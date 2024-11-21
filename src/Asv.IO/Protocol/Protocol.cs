using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;

namespace Asv.IO;

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
        var ar1 = requestQueryString.Split('&', '?','#',';');
        foreach (var row in ar1)
        {
            if (string.IsNullOrEmpty(row)) continue;
            var index = row.IndexOf('=');
            if (index < 0) continue;
            rc[Uri.UnescapeDataString(row[..index])] = Uri.UnescapeDataString(row[(index + 1)..]); // use Unescape only parts          
        }
        return rc;
    }

    public const string ProtocolQueryKey = "protocols";
    private const char ValuesDelimiter = ',';
    
    #endregion

    private readonly ImmutableArray<IProtocolFeature> _features;
    private readonly ImmutableDictionary<string, ParserFactoryDelegate> _parsers;
    private readonly ImmutableArray<ProtocolInfo> _protocols;
    private readonly ImmutableDictionary<string, PortFactoryDelegate> _ports;
    private readonly ImmutableArray<PortTypeInfo> _portInfos;
    private readonly ImmutableArray<IProtocolMessagePrinter> _printers;
    private readonly IProtocolCore _core;
    internal Protocol(
        ImmutableArray<IProtocolFeature> features, 
        ImmutableDictionary<string, ParserFactoryDelegate> parsers, 
        ImmutableArray<ProtocolInfo> protocols, 
        ImmutableDictionary<string,PortFactoryDelegate> ports, 
        ImmutableArray<PortTypeInfo> portInfos,
        ImmutableArray<IProtocolMessagePrinter> printers,
        IProtocolCore core)
    {
        _features = features;
        _parsers = parsers;
        _protocols = protocols;
        _ports = ports;
        _portInfos = portInfos;
        _printers = printers;
        _core = core;
    }

    public IProtocolCore Core => _core;
    public ImmutableArray<IProtocolFeature> Features => _features;
    public IEnumerable<PortTypeInfo> Ports => _portInfos;
    public IEnumerable<ProtocolInfo> Protocols => _protocols;
    public IProtocolPort CreatePort(Uri connectionString)
    {
        if (!_ports.TryGetValue(connectionString.Scheme, out var factory))
        {
            throw new InvalidOperationException($"Port type {connectionString.Scheme} not found");
        }
        var additionalArgs = ParseQueryString(connectionString.Fragment);
        var protoStr = additionalArgs[ProtocolQueryKey];


        ImmutableHashSet<string> protocols;
        if (protoStr != null)
        {
            protocols = protoStr.Split(ValuesDelimiter).ToImmutableHashSet();
        }
        else
        {
            protocols = _protocols.Select(x => x.Id).ToImmutableHashSet();
        }
        foreach (var protocol in protocols)
        {
            if (_parsers.ContainsKey(protocol) == false)
            {
                throw new InvalidOperationException($"Parser for protocol '{protocol}' not found");
            }
        }

        var selectedProtocol =  _protocols.Where(x => protocols.Contains(x.Id)).ToImmutableArray();
        
        var query = ParseQueryString(connectionString.Query);
        var args = new PortArgs
        {
            UserInfo = connectionString.UserInfo,
            Host = connectionString.Host,
            Port = connectionString.Port,
            Path = connectionString.AbsolutePath,
            Query = ParseQueryString(connectionString.Query)
        };
        return factory(args, _features, _parsers, selectedProtocol , _core);
    }

    public IProtocolParser CreateParser(string protocolId)
    {
        if (_parsers.TryGetValue(protocolId, out var factory))
        {
            return factory(_core);
        }
        throw new InvalidOperationException($"Parser for protocol '{protocolId}' not found");
    }

    public IProtocolRouter CreateRouter(string id)
    {
        return new ProtocolRouter(id,this);
    }

    public string? PrintMessage(IProtocolMessage message, PacketFormatting formatting = PacketFormatting.Inline)
    {
        return _printers
            .Where(x => x.CanPrint(message))
            .Select(x => x.Print(message, formatting))
            .FirstOrDefault();
    }
}