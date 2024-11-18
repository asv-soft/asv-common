using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Asv.IO;

//          userinfo       host      port
//          ┌──┴───┐ ┌──────┴──────┐ ┌┴─┐
//  tcp_s://john.doe@www.example.com:1234/forum/questions/?br=115200&timeout=1000#protocol=mavlink&feature
//  └─┬─┘   └─────────────┬─────────────┘└───────┬───────┘ └────────────┬────────────┘ └───────┬───────┘
//  scheme            authority                path                   query                 fragment

public class PortArgs
{
    public string? UserInfo { get; set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? Path { get; set; }
    public NameValueCollection Query { get; set; } = new();
}

public delegate IProtocolPort CreatePortDelegate(PortArgs args, IProtocolCore core, ImmutableArray<IProtocolProcessingFeature> features);

public interface IProtocolPortFactory
{
    IProtocolPort Create(Uri connectionString);
}

public static class ProtocolPortFactoryHelper
{
    public static IProtocolPort Create(this IProtocolPortFactory src, string connectionString)
    {
        return src.Create(new Uri(connectionString));
    }
}

public class ProtocolPortFactory:IProtocolPortFactory
{
    public ProtocolPortFactory(IProtocolCore core, IProtocolParserFactory parserFactory, ImmutableArray<IProtocolProcessingFeature> features)
    {
        
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
    
    
    
    public IProtocolPort Create(string connectionString, IProtocolCore core,
        IEnumerable<IProtocolProcessingFeature> filters, Func<IEnumerable<IProtocolParser>> parserFactory)
    {
        var uri = new Uri(connectionString);
        IProtocolPort? result = null;
        var scheme = uri.Scheme.ToLower();
        var parameters = ParseQueryString(uri.Query);
        switch (scheme)
        {
            case "tcp_c":
                
                break;
            default:
                break;
        }
        if (TcpClientProtocolPortConfig.TryParseFromUri(uri, out var tcp))
        {
            Debug.Assert(tcp != null, nameof(tcp) + " != null");
            if (tcp.IsServer)
            {
                result = new TcpServerPipePort(tcp, core);
            }
            else
            {
                result = new TcpClientProtocolPort(tcp, core,filters, parserFactory  );
            }
        }
        else if (UdpPortConfig.TryParseFromUri(uri, out var udp))
        {
            //result = new UdpPort(udp);
        }
        else if (SerialPortConfig.TryParseFromUri(uri, out var ser))
        {
            //result = new CustomSerialPort(ser, timeProvider, logger);
        }
        else
        {
            throw new Exception($"Connection string '{connectionString}' is invalid");
        }
        throw new Exception($"Connection string '{connectionString}' is invalid");
    }

    public IProtocolPort Create(Uri connectionString)
    {
        var scheme = connectionString.Scheme.Trim().ToLower();
        
    }
}