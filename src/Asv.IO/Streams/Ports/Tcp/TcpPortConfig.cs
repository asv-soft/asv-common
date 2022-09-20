using System;
using System.Net;

namespace Asv.IO
{
    public class TcpPortConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public bool IsServer { get; set; }
        public int ReconnectTimeout { get; set; } = 10_000;

        public static bool TryParseFromUri(Uri uri, out TcpPortConfig opt)
        {
            if (!"tcp".Equals(uri.Scheme, StringComparison.InvariantCultureIgnoreCase))
            {
                opt = null;
                return false;
            }
            var coll = PortFactory.ParseQueryString(uri.Query);
            opt = new TcpPortConfig
            {
                IsServer = bool.Parse(coll["srv"] ?? bool.FalseString),
                ReconnectTimeout = int.Parse(coll["rx_timeout"] ?? "10000"),
                Host = IPAddress.Parse(uri.Host).ToString(),
                Port = uri.Port,
            };

            return true;
        }

        public override string ToString()
        {
            if (IsServer)
            {
                return $"TCP_S {Host}:{Port}";
            }
            else
            {
                return $"TCP_C {Host}:{Port}";
            }
            
        }
    }
}
