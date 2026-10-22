using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Asv.IO.Test;

public class ProtocolConnectionDisplayTest
{
    [Theory]
    [InlineData(
        "udp://127.0.0.1:14550",
        "UDP · 127.0.0.1:14550",
        "UDP",
        "Local address: 127.0.0.1:14550"
    )]
    [InlineData(
        "tcp://127.0.0.1:5760",
        "TCP · 127.0.0.1:5760",
        "TCP client",
        "Remote address: 127.0.0.1:5760"
    )]
    [InlineData(
        "tcps://127.0.0.1:5760",
        "TCP · 127.0.0.1:5760",
        "TCP server",
        "Listen address: 127.0.0.1:5760"
    )]
    [InlineData("udp://[::1]:14550", "UDP · [::1]:14550", "UDP", "Local address: [::1]:14550")]
    [InlineData("tcp://[::1]:5760", "TCP · [::1]:5760", "TCP client", "Remote address: [::1]:5760")]
    [InlineData(
        "tcps://[::1]:5760",
        "TCP · [::1]:5760",
        "TCP server",
        "Listen address: [::1]:5760"
    )]
    public void Port_UsesCompactAddressAndFullDescription(
        string address,
        string expected,
        string transport,
        string endpoint
    )
    {
        using var port = CreatePort(address);
        Assert.Equal(expected, port.DisplayName);
        var discovery =
            transport == "UDP"
                ? "Remote endpoints: discovered from incoming packets\n"
                : string.Empty;
        Assert.Equal(
            $"Transport: {transport}\n{endpoint}\n{discovery}"
                + "Protocols: None\nReconnect interval: 5 s\n"
                + "TX queue: 100 messages\nRX queue: 100 messages\n"
                + $"Connection string: {port.Config}",
            port.Description
        );
        Assert.Empty(port.Endpoints);
    }

    [Fact]
    public void TcpClient_DescriptionMatchesDocumentedExample()
    {
        using var port = CreatePort("tcp://192.168.1.10:5760?reconnect=3000&tx_queue=200");

        Assert.Equal(
            """
            Transport: TCP client
            Remote address: 192.168.1.10:5760
            Protocols: None
            Reconnect interval: 3 s
            TX queue: 200 messages
            RX queue: 200 messages (inherited from TX)
            Connection string: tcp://192.168.1.10:5760/?reconnect=3000&tx_queue=200
            """,
            port.Description
        );
    }

    [Theory]
    [InlineData("udp://127.0.0.1:14550?name=Telemetry", "Telemetry")]
    [InlineData("tcp://127.0.0.1:5760?name=Telemetry", "Telemetry")]
    [InlineData("tcps://127.0.0.1:5760?name=Telemetry", "Telemetry")]
    [InlineData("serial:///dev/ttyUSB0?name=Telemetry", "Telemetry")]
    [InlineData("udp://127.0.0.1:14550?name=%20%20", "UDP · 127.0.0.1:14550")]
    public void Port_PrefersNonblankConfiguredName(string address, string expected)
    {
        using var port = CreatePort(address);
        Assert.Equal(expected, port.DisplayName);
        if (expected == "Telemetry")
        {
            Assert.Contains("Name: Telemetry", port.Description.Split('\n'));
        }
        else
        {
            Assert.DoesNotContain(port.Description.Split('\n'), line => line.StartsWith("Name:"));
        }
    }

    [Theory]
    [InlineData(
        "serial:///dev/ttyUSB0?br=115200",
        "Serial · ttyUSB0",
        "Device: /dev/ttyUSB0\nBaud rate: 115200 bit/s\nData bits: 8\nParity: None\nStop bits: 1"
    )]
    [InlineData(
        "serial:COM3?br=9600&data_bits=7&parity=Even&stop=Two",
        "Serial · COM3",
        "Device: COM3\nBaud rate: 9600 bit/s\nData bits: 7\nParity: Even\nStop bits: 2"
    )]
    [InlineData(
        "serial:COM4?br=57600&data_bits=5&parity=Odd&stop=OnePointFive",
        "Serial · COM4",
        "Device: COM4\nBaud rate: 57600 bit/s\nData bits: 5\nParity: Odd\nStop bits: 1.5"
    )]
    public void Serial_ShowsCompactNameAndFullSettings(string address, string name, string details)
    {
        using var port = CreatePort(address);
        Assert.Equal(name, port.DisplayName);
        Assert.Equal(
            $"Transport: Serial\n{details}\n"
                + "Protocols: None\nReconnect interval: 5 s\n"
                + "TX queue: 100 messages\nRX queue: 100 messages\n"
                + $"Connection string: {port.Config}",
            port.Description
        );
        Assert.Empty(port.Endpoints);
    }

    [Fact]
    public void Serial_AlternativeBackendDescribesEffectiveStopBits()
    {
        using var port = CreatePort("serial:COM3?version=2&stop=None");

        Assert.Contains("Stop bits: 1 (configured as None)", port.Description.Split('\n'));
        Assert.Contains("Version: 2", port.Description.Split('\n'));
        Assert.Equal($"Connection string: {port.Config}", port.Description.Split('\n')[^1]);
        Assert.Empty(port.Endpoints);
    }

    [Theory]
    [InlineData(-1, "Disabled")]
    [InlineData(0, "0 s")]
    public void Port_DescribesDisabledAndImmediateReconnect(int interval, string expected)
    {
        using var port = CreatePort($"tcp://127.0.0.1:5760?reconnect={interval}");

        Assert.Contains($"Reconnect interval: {expected}", port.Description.Split('\n'));
        Assert.False(port.IsEnabled.CurrentValue);
        Assert.Empty(port.Endpoints);
    }

    [Theory]
    [InlineData("tx_queue=200", "200 messages", "200 messages (inherited from TX)")]
    [InlineData("tx_queue=200&rx_queue=25", "200 messages", "25 messages")]
    [InlineData(
        "tx_queue=200&rx_queue=invalid",
        "200 messages",
        "200 messages (inherited from TX)"
    )]
    [InlineData("tx_queue=invalid&rx_queue=invalid", "100 messages", "100 messages")]
    [InlineData("tx_queue=0", "Unbounded", "Unbounded (inherited from TX)")]
    [InlineData("tx_queue=-1&rx_queue=0", "Unbounded", "Unbounded")]
    public void Port_DescribesEffectiveQueueCapacityAndInheritance(
        string query,
        string tx,
        string rx
    )
    {
        using var port = CreatePort($"tcp://127.0.0.1:5760?{query}");
        var lines = port.Description.Split('\n');
        Assert.Contains($"TX queue: {tx}", lines);
        Assert.Contains($"RX queue: {rx}", lines);
        Assert.Equal($"Connection string: {port.Config}", lines[^1]);
    }

    [Theory]
    [InlineData("tx_drop=true&rx_drop=false", "Drop oldest messages", "Wait for space")]
    [InlineData("tx_drop=false&rx_drop=true", "Wait for space", "Drop oldest messages")]
    [InlineData(
        "tx_queue=0&tx_drop=true&rx_drop=false",
        "Not applicable (unbounded queue)",
        "Not applicable (unbounded queue)"
    )]
    public void Port_DescribesExplicitOverflowPolicy(string query, string tx, string rx)
    {
        using var port = CreatePort($"udp://127.0.0.1:14550?{query}&rx_delay=12");
        var lines = port.Description.Split('\n');
        Assert.Contains($"TX queue overflow: {tx}", lines);
        Assert.Contains($"RX queue overflow: {rx}", lines);
        Assert.Contains("Empty read delay: 12 ms", lines);
        Assert.Empty(port.Endpoints);
    }

    [Theory]
    [InlineData("true", "Yes")]
    [InlineData("false", "No")]
    public void Port_DescribesConfiguredStateAndVersionWithoutStartingTransport(
        string enabled,
        string description
    )
    {
        using var port = CreatePort($"tcp://127.0.0.1:5760?enabled={enabled}&version=2");
        var lines = port.Description.Split('\n');
        Assert.Contains($"Enabled in configuration: {description}", lines);
        Assert.Contains("Version: 2", lines);
        Assert.False(port.IsEnabled.CurrentValue);
        Assert.Empty(port.Endpoints);
    }

    [Fact]
    public void Udp_DescribesDistinctSortedLegacyAndRepeatedRemoteEndpoints()
    {
        using var port = CreatePort(
            "udp://127.0.0.1:14550?rhost=127.0.0.2&rport=14552"
                + "&remote=127.0.0.3:14553&remote=127.0.0.2:14552&remote=[::1]:14554"
                + "&remote=invalid"
        );

        Assert.Contains(
            "Allowed remote endpoints:\n  127.0.0.2:14552\n  127.0.0.3:14553\n  [::1]:14554\n",
            port.Description
        );
        Assert.DoesNotContain("Remote endpoints: discovered", port.Description);
        Assert.Empty(port.Endpoints);
    }

    [Fact]
    public void Udp_DescriptionUsesRetainedRemoteEndpointsAfterConfigurationMutationAndDisposal()
    {
        var config = new UdpProtocolPortConfig(
            new Uri("udp://127.0.0.1:14550?rhost=127.0.0.2&rport=14552")
        );
        using var port = new UdpProtocolPort(config, Context(), new Statistic());

        config.Query.Remove(UdpProtocolPortConfig.RemoteHostKey);
        config.Query.Remove(UdpProtocolPortConfig.RemotePortKey);

        var description = port.Description;
        Assert.Contains("Allowed remote endpoints:\n  127.0.0.2:14552\n", description);
        Assert.DoesNotContain("Remote endpoints: discovered", description);
        Assert.Equal($"Connection string: {config}", description.Split('\n')[^1]);
        Assert.Empty(port.Endpoints);

        port.Dispose();

        Assert.Equal(description, port.Description);
    }

    [Theory]
    [InlineData("maxConnection=42", "Pending connection queue: 42")]
    [InlineData("maxConnection=invalid", null)]
    public void TcpServer_DescribesListenBacklogWithoutClaimingAnActiveConnectionLimit(
        string query,
        string? expected
    )
    {
        using var port = CreatePort($"tcps://0.0.0.0:5760?{query}");
        var lines = port.Description.Split('\n');
        if (expected != null)
        {
            Assert.Contains(expected, lines);
        }
        else
        {
            Assert.DoesNotContain(lines, line => line.StartsWith("Pending connection queue:"));
        }
        Assert.DoesNotContain("Maximum connections", port.Description);
        Assert.Contains("Listen address: 0.0.0.0:5760", lines);
        Assert.Empty(port.Endpoints);
    }

    [Theory]
    [InlineData("", "Alpha protocol, Zeta protocol", 2)]
    [InlineData(
        "#protocols=zeta&protocols=missing&protocols=alpha",
        "Alpha protocol, Zeta protocol",
        2
    )]
    [InlineData("#protocols=zeta", "Zeta protocol", 1)]
    [InlineData("#protocols=missing", "None", 0)]
    public void Port_DescribesOnlyRegisteredEnabledProtocolsInStableOrder(
        string fragment,
        string expected,
        int count
    )
    {
        var factory = Protocol.Create(builder =>
        {
            builder.Protocols.Register(
                new ProtocolInfo("zeta", "Zeta protocol"),
                (_, _) =>
                    throw new InvalidOperationException(
                        "Displaying a port must not create parsers."
                    )
            );
            builder.Protocols.Register(
                new ProtocolInfo("alpha", "Alpha protocol"),
                (_, _) =>
                    throw new InvalidOperationException(
                        "Displaying a port must not create parsers."
                    )
            );
        });
        using var port = CreatePort($"tcp://127.0.0.1:5760{fragment}", (IProtocolContext)factory);

        Assert.Contains($"Protocols: {expected}", port.Description.Split('\n'));
        Assert.Equal(count, port.EnabledProtocols.Count());
        Assert.Empty(port.Endpoints);
    }

    [Theory]
    [InlineData("ru-RU")]
    [InlineData("en-US")]
    public void Serial_FormatsParsedSettingsInvariantlyAndKeepsConnectionStringLast(
        string cultureName
    )
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            using var port = CreatePort(
                "serial:COM3?name=Telemetry%20link&reconnect=1234&stop=OnePointFive"
                    + "&tx_size=4096&rx_size=8192&tx_timout=-1&rx_timout=1500#protocols=unknown"
            );
            var lines = port.Description.Split('\n');
            Assert.Contains("Name: Telemetry link", lines);
            Assert.Contains("Reconnect interval: 1.234 s", lines);
            Assert.Contains("Stop bits: 1.5", lines);
            Assert.Contains("TX buffer size: 4096 bytes", lines);
            Assert.Contains("RX buffer size: 8192 bytes", lines);
            Assert.Contains("TX timeout: Infinite", lines);
            Assert.Contains("RX timeout: 1.5 s", lines);
            Assert.Equal($"Connection string: {port.Config}", lines[^1]);
            Assert.Contains("#protocols=unknown", lines[^1]);
            Assert.Equal("Telemetry link", port.DisplayName);
            Assert.Empty(port.Endpoints);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Theory]
    [InlineData("udp://127.0.0.1:14550?remote=127.0.0.2:14551")]
    [InlineData("tcp://127.0.0.1:5760?reconnect=2500")]
    [InlineData("tcps://0.0.0.0:5760?maxConnection=42")]
    [InlineData("serial:COM3?br=9600")]
    public void Port_DescriptionRemainsAvailableAfterDisposal(string address)
    {
        using var port = CreatePort(address);
        var description = port.Description;
        var displayName = port.DisplayName;

        port.Dispose();

        Assert.Equal(description, port.Description);
        Assert.Equal(displayName, port.DisplayName);
        Assert.Equal($"Connection string: {port.Config}", port.Description.Split('\n')[^1]);
    }

    [Fact]
    public async Task UdpEndpoint_DisplaySurvivesSocketDisposal()
    {
        using var socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp
        );
        var endpoint = new UdpSocketProtocolEndpoint(
            socket,
            new IPEndPoint(IPAddress.Loopback, 14551),
            "technical-endpoint-id",
            new UdpProtocolPortConfig(new Uri("udp://127.0.0.1:14550")),
            [],
            Context(),
            new Statistic()
        );
        Assert.Equal("127.0.0.1:14551", endpoint.DisplayName);
        var description = endpoint.Description;
        Assert.Contains("technical-endpoint-id", description);
        await endpoint.DisposeAsync();
        socket.Dispose();
        Assert.Equal("127.0.0.1:14551", endpoint.DisplayName);
        Assert.Equal(description, endpoint.Description);
    }

    [Fact]
    public async Task TcpEndpoint_FallbackMetadataSurvivesSocketDisposal()
    {
        using var socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp
        );
        var endpoint = new TcpClientSocketProtocolEndpoint(
            socket,
            "technical-tcp-id",
            new TcpClientProtocolPortConfig(new Uri("tcp://127.0.0.1:5760")),
            [],
            Context(),
            new Statistic()
        );
        Assert.Equal("technical-tcp-id", endpoint.DisplayName);
        var description = endpoint.Description;
        Assert.StartsWith("Transport: TCP client\n", description);
        await endpoint.DisposeAsync();
        socket.Dispose();
        Assert.Equal("technical-tcp-id", endpoint.DisplayName);
        Assert.Equal(description, endpoint.Description);
    }

    [Fact]
    public void VirtualEndpoint_HasReadableMetadata()
    {
        using var endpoint = new VirtualPort("simulation", Context(), _ => true, new Statistic());
        Assert.Equal("simulation", endpoint.DisplayName);
        Assert.Equal(
            "Transport: Virtual\nEndpoint: simulation\nConnection string: virtual://simulation",
            endpoint.Description
        );
        endpoint.Dispose();
        Assert.Equal(
            "Transport: Virtual\nEndpoint: simulation\nConnection string: virtual://simulation",
            endpoint.Description
        );
    }

    [Fact]
    public void CustomConnection_FallsBackToItsId()
    {
        using var connection = new TestConnection("custom-transport");
        IProtocolConnection contract = connection;
        Assert.Equal(connection.Id, contract.DisplayName);
        Assert.Equal(connection.Id, contract.Description);
    }

    [Fact]
    public void CustomPort_DescribesParsedConfigurationWithoutStartingTransport()
    {
        var config = new ProtocolPortConfig(new Uri("custom://127.0.0.1:1234/device"))
        {
            Name = "Custom transport",
            ReconnectTimeoutMs = 2500,
        };
        using var port = new TestPort(config);

        Assert.Equal(
            "Transport: custom\nAddress: 127.0.0.1:1234\nPath: /device\n"
                + "Name: Custom transport\nProtocols: None\nReconnect interval: 2.5 s\n"
                + "TX queue: 100 messages\nRX queue: 100 messages\n"
                + $"Connection string: {config}",
            port.Description
        );
        Assert.Equal("Custom transport", port.DisplayName);
        Assert.Empty(port.Endpoints);
    }

    private static IProtocolContext Context() => (IProtocolContext)Protocol.Create(_ => { });

    private static IProtocolPort CreatePort(string address, IProtocolContext? context = null)
    {
        var uri = new Uri(address);
        context ??= Context();
        return uri.Scheme switch
        {
            "udp" => new UdpProtocolPort(new UdpProtocolPortConfig(uri), context, new Statistic()),
            "tcp" => new TcpClientProtocolPort(
                new TcpClientProtocolPortConfig(uri),
                context,
                new Statistic()
            ),
            "tcps" => new TcpServerProtocolPort(
                new TcpServerProtocolPortConfig(uri),
                context,
                new Statistic()
            ),
            "serial" => new SerialProtocolPort(
                new SerialProtocolPortConfig(uri),
                context,
                new Statistic()
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(address)),
        };
    }

    private sealed class TestConnection(string id) : ProtocolConnection(id, Context())
    {
        public override ValueTask Send(
            IProtocolMessage message,
            CancellationToken cancel = default
        ) => ValueTask.CompletedTask;
    }

    private sealed class TestPort(ProtocolPortConfig config)
        : ProtocolPort<ProtocolPortConfig>("custom-port", config, Context(), false)
    {
        public override PortTypeInfo TypeInfo => new("custom", "Custom transport");

        protected override void InternalSafeDisable() { }

        protected override void InternalSafeEnable(CancellationToken token) =>
            throw new InvalidOperationException("Displaying a port must not start its transport.");
    }
}
