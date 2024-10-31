using System;
using System.Collections.Specialized;
using Asv.Common;

namespace Asv.IO
{
    public enum PortState
    {
        Disabled,
        Connecting,
        Error,
        Connected,
    }

    public enum PortType
    {
        Serial,
        Udp,
        Tcp,
    }

    public interface IPort : IDataStream, IDisposable
    {
        PortType PortType { get; }
        TimeSpan ReconnectTimeout { get; set; }
        IRxValue<bool> IsEnabled { get; }
        IRxValue<PortState> State { get; }
        IRxValue<Exception> Error { get; }
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
                if (string.IsNullOrEmpty(row))
                {
                    continue;
                }

                var index = row.IndexOf('=');
                if (index < 0)
                {
                    continue;
                }

                rc[Uri.UnescapeDataString(row.Substring(0, index))] = Uri.UnescapeDataString(
                    row.Substring(index + 1)
                ); // use Unescape only parts
            }

            return rc;
        }

        public static IPort Create(string connectionString, bool enabled = false)
        {
            var uri = new Uri(connectionString);
            IPort result = null;
            if (TcpPortConfig.TryParseFromUri(uri, out var tcp))
            {
                if (tcp.IsServer)
                {
                    result = new TcpServerPort(tcp);
                }
                else
                {
                    result = new TcpClientPort(tcp);
                }
            }
            else if (UdpPortConfig.TryParseFromUri(uri, out var udp))
            {
                result = new UdpPort(udp);
            }
            else if (SerialPortConfig.TryParseFromUri(uri, out var ser))
            {
                result = new CustomSerialPort(ser);
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
