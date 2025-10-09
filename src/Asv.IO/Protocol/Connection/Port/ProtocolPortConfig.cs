using System;
using System.Collections.Specialized;
using System.Net;
using System.Web;

namespace Asv.IO;

/// <summary>
///          userinfo       host      port
///          ┌──┴───┐ ┌──────┴──────┐ ┌┴─┐
///  tcp_s://john.doe@www.example.com:1234/forum/questions/?br=115200&timeout=1000#protocol=mavlink&name=Port1&enabled=true
///  └─┬─┘   └─────────────┬─────────────┘└───────┬───────┘ └────────────┬───────┘ └───────┬──────────────────────────────┘
///  scheme            authority                path                   query                 fragment
/// </summary>
public class ProtocolPortConfig(Uri connectionString) : ICloneable
{
    public const string VersionKey = "version";
    public const int DefaultVersion = 1;
    public uint Version
    {
        get =>
            uint.TryParse(Query[VersionKey], out var value)
                ? value
                : DefaultVersion;
        set => Query.Set(VersionKey, value.ToString());
    }

    public const string NameQueryKey = "name";
    public string? Name
    {
        get => Query[NameQueryKey];
        set => Query.Set(NameQueryKey, value);
    }

    public string Scheme { get; set; } = connectionString.Scheme;

    public const string ProtocolQueryKey = "protocols";
    public string[]? EnabledProtocols
    {
        get => Fragment.GetValues(ProtocolQueryKey);
        set
        {
            if (value == null)
            {
                Fragment.Remove(ProtocolQueryKey);
            }
            else
            {
                foreach (var s in value)
                {
                    Fragment.Add(ProtocolQueryKey, s);
                }
            }
        }
    }
    public string? UserInfo { get; set; } = connectionString.UserInfo;
    public string? Host { get; set; } = connectionString.Host;
    public int? Port { get; set; } = connectionString.Port;
    public string? Path { get; set; } = connectionString.AbsolutePath;

    public const string EnabledKey = "enabled";
    public bool IsEnabled
    {
        get => !bool.TryParse(Query[EnabledKey], out var value) || value;
        set => Query.Set(EnabledKey, value.ToString());
    }

    public const string ReconnectTimeoutKey = "reconnect";
    public const int ReconnectTimeoutDefault = 5000;
    public int ReconnectTimeoutMs
    {
        get =>
            int.TryParse(Query[ReconnectTimeoutKey], out var value)
                ? value
                : ReconnectTimeoutDefault;
        set => Query.Set(ReconnectTimeoutKey, value.ToString());
    }

    private const string TxQueueSizeKey = "tx_queue";
    private const int TxQueueSizeDefault = 100;
    public int TxQueueSize
    {
        get => int.TryParse(Query[TxQueueSizeKey], out var value) ? value : TxQueueSizeDefault;
        set => Query.Set(TxQueueSizeKey, value.ToString());
    }

    private const string ReadEmptyLoopDelayMsKey = "rx_delay";
    private const int ReadEmptyLoopDelayMsDefault = 30;
    public int ReadEmptyLoopDelayMs
    {
        get =>
            int.TryParse(Query[ReadEmptyLoopDelayMsKey], out var value)
                ? value
                : ReadEmptyLoopDelayMsDefault;
        set => Query.Set(ReadEmptyLoopDelayMsKey, value.ToString());
    }
    private const string DropMessageWhenFullTxQueueKey = "tx_drop";
    public bool DropMessageWhenFullTxQueue
    {
        get => bool.TryParse(Query[DropMessageWhenFullTxQueueKey], out var value) && value;
        set => Query.Set(DropMessageWhenFullTxQueueKey, value.ToString());
    }
    private const string DropMessageWhenFullRxQueueKey = "rx_drop";
    public bool DropMessageWhenFullRxQueue
    {
        get => bool.TryParse(Query[DropMessageWhenFullRxQueueKey], out var value) && value;
        set => Query.Set(DropMessageWhenFullRxQueueKey, value.ToString());
    }

    private const string RxQueueSizeKey = "tx_queue";
    private const int RxQueueSizeDefault = 100;
    public int RxQueueSize
    {
        get => int.TryParse(Query[RxQueueSizeKey], out var value) ? value : RxQueueSizeDefault;
        set => Query.Set(RxQueueSizeKey, value.ToString());
    }
    private const string SendBufferSizeKey = "tx_size";
    private const int SendBufferSizeDefault = 64 * 1024;
    public int WriteBufferSize
    {
        get =>
            int.TryParse(Query[SendBufferSizeKey], out var value) ? value : SendBufferSizeDefault;
        set => Query.Set(SendBufferSizeKey, value.ToString());
    }
    private const string SendTimeoutKey = "tx_timout";
    private const int SendTimeoutDefault = 1000;
    public int WriteTimeout
    {
        get => int.TryParse(Query[SendTimeoutKey], out var value) ? value : SendTimeoutDefault;
        set => Query.Set(SendTimeoutKey, value.ToString());
    }

    private const string ReceiveBufferSizeKey = "rx_size";
    private const int ReceiveBufferSizeDefault = 64 * 1024;

    public int ReadBufferSize
    {
        get =>
            int.TryParse(Query[ReceiveBufferSizeKey], out var value)
                ? value
                : ReceiveBufferSizeDefault;
        set => Query.Set(ReceiveBufferSizeKey, value.ToString());
    }
    private const string ReceiveTimeoutKey = "rx_timout";
    private const int ReceiveTimeoutDefault = 1000;

    public int ReadTimeout
    {
        get =>
            int.TryParse(Query[ReceiveTimeoutKey], out var value) ? value : ReceiveTimeoutDefault;
        set => Query.Set(ReceiveTimeoutKey, value.ToString());
    }

    public NameValueCollection Query { get; } =
        HttpUtility.ParseQueryString(connectionString.Query);
    public NameValueCollection Fragment { get; } =
        HttpUtility.ParseQueryString(connectionString.Fragment.Trim('#'));

    public Uri AsUri()
    {
        var builder = new UriBuilder
        {
            Fragment = Fragment.ToString(),
            Query = Query.ToString(),
            Path = Path,
            Host = Host,
            Scheme = Scheme,
            UserName = UserInfo,
        };
        if (Port.HasValue)
        {
            builder.Port = Port.Value;
        }
        return builder.Uri;
    }

    public IPEndPoint CheckAndGetLocalHost()
    {
        return CheckIpEndpoint(Host, Port);
    }

    public static IPEndPoint CheckIpEndpoint(string? host, int? port)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(port);

        if (port < IPEndPoint.MinPort && port > IPEndPoint.MaxPort)
        {
            throw new ArgumentOutOfRangeException(
                nameof(port),
                port,
                $"Port must be in range {IPEndPoint.MinPort}..{IPEndPoint.MaxPort}"
            );
        }

        if (IPAddress.TryParse(host, out var ipAddress))
        {
            return new IPEndPoint(ipAddress, port.Value);
        }
        else
        {
            var addresses = Dns.GetHostAddresses(host);
            return new IPEndPoint(addresses[0], port.Value);
        }
    }

    public override string ToString()
    {
        return AsUri().ToString();
    }

    public virtual object Clone() => new ProtocolPortConfig(AsUri());
}
