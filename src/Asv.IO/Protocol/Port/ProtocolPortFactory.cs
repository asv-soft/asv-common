using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Asv.IO;

public interface IProtocolPortFactory
{
    IProtocolPort Create(string connectionString, IPipeCore core,
        IEnumerable<IProtocolRouteFilter> filters, Func<IEnumerable<IProtocolParser>> parserFactory);
}

public class ProtocolPortFactory:IProtocolPortFactory
{
    public ProtocolPortFactory(IPipeCore core)
    {
        
    }
    
    public static void RegisterPortFactory(string scheme, Func<Uri, IProtocolPort> factory)
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
    
    
    
    public IProtocolPort Create(string connectionString, IPipeCore core,
        IEnumerable<IProtocolRouteFilter> filters, Func<IEnumerable<IProtocolParser>> parserFactory)
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

}