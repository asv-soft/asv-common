using System;
using System.Collections.Specialized;
using Asv.Common;
using Microsoft.Extensions.Logging;
using R3;

namespace Asv.IO
{
    public enum PortState
    {
        Disabled,
        Connecting,
        Error,
        Connected
    }

    public enum PortType
    {
        Serial,
        Udp,
        Tcp
    }


    public interface IPort: IDataStream, IDisposable
    {
        PortType PortType { get; }
        TimeSpan ReconnectTimeout { get; set; }
        ReadOnlyReactiveProperty<bool> IsEnabled { get; }
        ReadOnlyReactiveProperty<PortState> State { get; }
        ReadOnlyReactiveProperty<Exception?> Error { get; }
        void Enable();
        void Disable();
    }

    public static class PortFactory
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


        public static IPort Create(string connectionString, bool enabled = false, TimeProvider? timeProvider = null, ILogger? logger = null)
        {
            var uri = new Uri(connectionString);
            IPort result = null;
            if (TcpPortConfig.TryParseFromUri(uri, out var tcp))
            {
                if (tcp.IsServer)
                {
                    result = new TcpServerPort(tcp, timeProvider, logger);
                }
                else
                {
                    result = new TcpClientPort(tcp, timeProvider, logger);
                }
            }
            else if (UdpPortConfig.TryParseFromUri(uri, out var udp))
            {
                result = new UdpPort(udp);
            }
            else if (SerialPortConfig.TryParseFromUri(uri, out var ser))
            {
                result = new CustomSerialPort(ser, timeProvider, logger);
            }
            else
            {
                throw new Exception($"Connection string '{connectionString}' is invalid");
            }
            if (enabled)
            {
                result.Enable();
            }
            return result;
        }
    }
}
