using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;

namespace Asv.IO;

internal static class ProtocolConnectionDisplay
{
    public static string Network(string transport, ProtocolPortConfig config) =>
        $"{transport} · {NetworkAddress(config)}";

    private static string NetworkAddress(ProtocolPortConfig config)
    {
        var host = config.Host ?? string.Empty;
        if (host.Contains(':') && !host.StartsWith('['))
        {
            host = $"[{host}]";
        }
        return $"{host}:{config.Port?.ToString(CultureInfo.InvariantCulture)}";
    }

    public static string PortDescription(
        ProtocolPortConfig config,
        IEnumerable<ProtocolInfo> protocols,
        IEnumerable<IPEndPoint>? udpRemoteEndpoints = null
    )
    {
        var text = new StringBuilder();
        switch (config)
        {
            case TcpClientProtocolPortConfig:
                text.Append("Transport: TCP client\nRemote address: ");
                text.Append(NetworkAddress(config)).Append('\n');
                break;
            case TcpServerProtocolPortConfig server:
                text.Append("Transport: TCP server\nListen address: ");
                text.Append(NetworkAddress(config)).Append('\n');
                if (server.MaxConnection is { } backlog)
                {
                    text.Append(
                        CultureInfo.InvariantCulture,
                        $"Pending connection queue: {backlog}\n"
                    );
                }
                break;
            case UdpProtocolPortConfig udp:
                text.Append("Transport: UDP\nLocal address: ");
                text.Append(NetworkAddress(config)).Append('\n');
                AppendRemoteEndpoints(text, udpRemoteEndpoints ?? udp.GetRemoteEndpoints());
                break;
            case SerialProtocolPortConfig serial:
                text.Append("Transport: Serial\nDevice: ").Append(serial.PortName).Append('\n');
                text.Append(
                    CultureInfo.InvariantCulture,
                    $"Baud rate: {serial.BoundRate} bit/s\nData bits: {serial.DataBits}\n"
                );
                text.Append("Parity: ").Append(serial.Parity).Append('\n');
                text.Append("Stop bits: ")
                    .Append(
                        serial.Version == 2 && serial.StopBits == StopBits.None
                            ? "1 (configured as None)"
                            : StopBitsValue(serial.StopBits)
                    )
                    .Append('\n');
                break;
            default:
                text.Append("Transport: ").Append(config.Scheme).Append('\n');
                if (!string.IsNullOrEmpty(config.Host))
                {
                    text.Append("Address: ").Append(NetworkAddress(config)).Append('\n');
                }
                if (!string.IsNullOrEmpty(config.Path) && config.Path != "/")
                {
                    text.Append("Path: ").Append(config.Path).Append('\n');
                }
                break;
        }

        if (!string.IsNullOrWhiteSpace(config.Name))
        {
            text.Append("Name: ").Append(config.Name).Append('\n');
        }
        if (config.Query[ProtocolPortConfig.EnabledKey] != null)
        {
            text.Append("Enabled in configuration: ")
                .Append(config.IsEnabled ? "Yes" : "No")
                .Append('\n');
        }
        if (config.Query[ProtocolPortConfig.VersionKey] != null)
        {
            text.Append(CultureInfo.InvariantCulture, $"Version: {config.Version}\n");
        }

        var names = string.Join(", ", protocols.Select(x => x.Name).Order(StringComparer.Ordinal));
        text.Append("Protocols: ").Append(names.Length == 0 ? "None" : names).Append('\n');
        text.Append("Reconnect interval: ")
            .Append(
                config.ReconnectTimeoutMs == -1 ? "Disabled" : Seconds(config.ReconnectTimeoutMs)
            )
            .Append('\n');
        text.Append("TX queue: ").Append(QueueSize(config.TxQueueSize)).Append('\n');
        text.Append("RX queue: ").Append(QueueSize(config.RxQueueSize));
        if (
            !int.TryParse(config.Query["rx_queue"], out _)
            && int.TryParse(config.Query["tx_queue"], out _)
        )
        {
            text.Append(" (inherited from TX)");
        }
        text.Append('\n');

        if (config.Query["tx_drop"] != null)
        {
            text.Append("TX queue overflow: ")
                .Append(Overflow(config.TxQueueSize, config.DropMessageWhenFullTxQueue))
                .Append('\n');
        }
        if (config.Query["rx_drop"] != null)
        {
            text.Append("RX queue overflow: ")
                .Append(Overflow(config.RxQueueSize, config.DropMessageWhenFullRxQueue))
                .Append('\n');
        }
        if (config.Query["rx_delay"] != null)
        {
            text.Append(
                CultureInfo.InvariantCulture,
                $"Empty read delay: {config.ReadEmptyLoopDelayMs} ms\n"
            );
        }
        if (config is SerialProtocolPortConfig)
        {
            AppendSerialOptions(text, config);
        }

        return text.Append("Connection string: ").Append(config).ToString();
    }

    public static string VirtualDescription(string id) =>
        $"Transport: Virtual\nEndpoint: {id}\nConnection string: virtual://{id}";

    private static void AppendRemoteEndpoints(StringBuilder text, IEnumerable<IPEndPoint> endpoints)
    {
        var addresses = endpoints
            .Select(x => x.ToString())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (addresses.Length == 0)
        {
            text.Append("Remote endpoints: discovered from incoming packets\n");
            return;
        }
        text.Append("Allowed remote endpoints:\n");
        foreach (var address in addresses)
        {
            text.Append("  ").Append(address).Append('\n');
        }
    }

    private static void AppendSerialOptions(StringBuilder text, ProtocolPortConfig config)
    {
        if (config.Query["tx_size"] != null)
        {
            text.Append(
                CultureInfo.InvariantCulture,
                $"TX buffer size: {config.WriteBufferSize} bytes\n"
            );
        }
        if (config.Query["rx_size"] != null)
        {
            text.Append(
                CultureInfo.InvariantCulture,
                $"RX buffer size: {config.ReadBufferSize} bytes\n"
            );
        }
        if (config.Query["tx_timout"] != null)
        {
            text.Append("TX timeout: ").Append(Timeout(config.WriteTimeout)).Append('\n');
        }
        if (config.Query["rx_timout"] != null)
        {
            text.Append("RX timeout: ").Append(Timeout(config.ReadTimeout)).Append('\n');
        }
    }

    private static string QueueSize(int count) =>
        count <= 0 ? "Unbounded" : count.ToString(CultureInfo.InvariantCulture) + " messages";

    private static string Overflow(int count, bool drop) =>
        count <= 0 ? "Not applicable (unbounded queue)"
        : drop ? "Drop oldest messages"
        : "Wait for space";

    private static string Seconds(int milliseconds) =>
        (milliseconds / 1000d).ToString("0.###", CultureInfo.InvariantCulture) + " s";

    private static string Timeout(int milliseconds) =>
        milliseconds == -1 ? "Infinite" : Seconds(milliseconds);

    private static string StopBitsValue(StopBits value) =>
        value switch
        {
            StopBits.None => "0",
            StopBits.One => "1",
            StopBits.OnePointFive => "1.5",
            StopBits.Two => "2",
            _ => value.ToString(),
        };

    public static string SerialName(SerialProtocolPortConfig config) =>
        Path.GetFileName((config.PortName ?? string.Empty).Replace('\\', '/'));

    public static string SerialDescription(SerialProtocolPortConfig config)
    {
        var parity = config.Parity switch
        {
            Parity.None => "N",
            Parity.Odd => "O",
            Parity.Even => "E",
            Parity.Mark => "M",
            Parity.Space => "S",
            _ => config.Parity.ToString(),
        };
        var stopBits = StopBitsValue(config.StopBits);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{config.PortName} · {config.BoundRate} · {config.DataBits}{parity}{stopBits}\n{config}"
        );
    }
}
