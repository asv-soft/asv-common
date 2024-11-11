using System;
using System.Net;

namespace Asv.IO;

public class TcpPipePortConfig:PipePortConfig
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 7341;
    public bool IsServer { get; set; } = false;
    
    public override bool TryValidate(out string? error)
    {
        if (base.TryValidate(out error) == false) return false;
        if (IPAddress.TryParse(Host, out var ip) == false)
        {
            error = $"Host '{Host}' must be valid ip address";
            return false;
        }
        if (Port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
        {
            error = $"Port must be in range {IPEndPoint.MinPort}-{IPEndPoint.MaxPort}";
            return false;
        }
        error = null;
        return true;
    }
    
    public static bool TryParseFromUri(Uri uri, out TcpPipePortConfig? opt)
    {
        if (!"tcp".Equals(uri.Scheme, StringComparison.InvariantCultureIgnoreCase))
        {
            opt = null;
            return false;
        }
        var coll = PortFactory.ParseQueryString(uri.Query);
        opt = new TcpPipePortConfig
        {
            IsServer = bool.Parse(coll["srv"] ?? bool.FalseString),
            ReconnectTimeoutMs = int.Parse(coll["rx_timeout"] ?? "10000"),
            Host = IPAddress.Parse(uri.Host).ToString(),
            Port = uri.Port,
        };

        return true;
    }
}