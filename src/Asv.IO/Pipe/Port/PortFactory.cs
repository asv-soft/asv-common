using System;
using System.Collections.Specialized;
using System.Net;

namespace Asv.IO;

public static class PipePortFactory
{
    public static NameValueCollection ParseQueryString(string requestQueryString)
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
    
    public static IPipePort Create(string connectionString, IPipeCore core)
    {
        var uri = new Uri(connectionString);
        IPipePort? result = null;
        if (TcpPipePortConfig.TryParseFromUri(uri, out var tcp))
        {
            if (tcp.IsServer)
            {
                result = new TcpServerPipePort(tcp, core);
            }
            else
            {
                result = new TcpClientPipePort(tcp, core);
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
      
        return result;
    }
}