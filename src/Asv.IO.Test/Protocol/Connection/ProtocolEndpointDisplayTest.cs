using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using R3;
using Xunit;

namespace Asv.IO.Test;

public class ProtocolEndpointDisplayTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TcpEndpoint_DescribesActualRoleAndSocketAddressesAndKeepsSnapshot(bool server)
    {
        using var listener = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp
        );
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        using var client = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp
        );
        var accepted = listener.AcceptAsync(TestContext.Current.CancellationToken);
        await client.ConnectAsync(listener.LocalEndPoint!, TestContext.Current.CancellationToken);
        using var peer = await accepted;
        var socket = server ? peer : client;
        var localAddress = socket.LocalEndPoint!.ToString();
        var remoteAddress = socket.RemoteEndPoint!.ToString();
        ProtocolPortConfig config = server
            ? new TcpClientProtocolPortConfig(new Uri("tcp://192.0.2.1:5760?reconnect=1500"))
            : new TcpServerProtocolPortConfig(new Uri("tcps://192.0.2.2:5761?reconnect=1500"));
        var originalConnectionString = config.ToString();
        await using TcpSocketProtocolEndpoint endpoint = server
            ? new TcpServerSocketProtocolEndpoint(
                socket,
                "accepted-peer",
                config,
                [],
                Context(),
                new Statistic()
            )
            : new TcpClientSocketProtocolEndpoint(
                socket,
                "connected-peer",
                config,
                [],
                Context(),
                new Statistic()
            );
        var description = endpoint.Description;

        Assert.Equal(
            $"Transport: TCP {(server ? "server" : "client")}\nEndpoint: {endpoint.Id}\n"
                + $"Local address: {localAddress}\nRemote address: {remoteAddress}\n"
                + "Protocols: None\nIdle timeout: 1.5 s\n"
                + "TX queue: 100 messages\nRX queue: 100 messages\n"
                + $"Connection string: {originalConnectionString}",
            description
        );
        Assert.Equal(remoteAddress, endpoint.DisplayName);
        config.Host = "203.0.113.3";
        config.Port = 9999;
        config.ReconnectTimeoutMs = 0;
        config.TxQueueSize = 25;
        await endpoint.DisposeAsync();

        Assert.True(endpoint.IsDisposed);
        Assert.Equal(description, endpoint.Description);
        Assert.Equal(remoteAddress, endpoint.DisplayName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UdpEndpoint_DescribesActualPeerAndParsersAndKeepsSnapshot(bool bound)
    {
        using var socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp
        );
        if (bound)
        {
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        }
        var localAddress = socket.LocalEndPoint?.ToString() ?? "Not bound";
        var config = new UdpProtocolPortConfig(
            new Uri("udp://192.0.2.1:14550?remote=192.0.2.2:14551#protocols=missing")
        );
        var connectionString = config.ToString();
        var zeta = new MetadataParser("zeta", "Zeta protocol");
        var alpha = new MetadataParser("alpha", "Alpha protocol");
        await using var endpoint = new UdpSocketProtocolEndpoint(
            socket,
            new IPEndPoint(IPAddress.Loopback, 14552),
            "received-peer",
            config,
            [zeta, alpha],
            Context(),
            new Statistic()
        );
        var description = endpoint.Description;

        Assert.Equal(
            $"Transport: UDP\nEndpoint: received-peer\nLocal address: {localAddress}\n"
                + "Remote address: 127.0.0.1:14552\nProtocols: Alpha protocol, Zeta protocol\n"
                + "Idle timeout: 5 s\nTX queue: 100 messages\nRX queue: 100 messages\n"
                + $"Connection string: {connectionString}",
            description
        );
        Assert.Equal("127.0.0.1:14552", endpoint.DisplayName);
        config.Host = "203.0.113.1";
        config.Query["remote"] = "203.0.113.2:1234";
        config.RxQueueSize = 15;
        await endpoint.DisposeAsync();
        socket.Dispose();

        Assert.Equal(description, endpoint.Description);
        Assert.Equal("127.0.0.1:14552", endpoint.DisplayName);
        Assert.Equal(1, alpha.Disposals);
        Assert.Equal(1, zeta.Disposals);
    }

    [Fact]
    public void NetworkEndpoint_FormatsIpv6Addresses()
    {
        var config = new TcpClientProtocolPortConfig(
            new Uri("tcp://[::1]:5760?reconnect=3000&tx_queue=200")
        );
        var description = ProtocolConnectionDisplay.NetworkEndpointDescription(
            "TCP client",
            "tcp-peer",
            config,
            [],
            new IPEndPoint(IPAddress.IPv6Loopback, 50000),
            new IPEndPoint(IPAddress.IPv6Loopback, 5760)
        );

        Assert.Equal(
            "Transport: TCP client\nEndpoint: tcp-peer\nLocal address: [::1]:50000\n"
                + "Remote address: [::1]:5760\nProtocols: None\nIdle timeout: 3 s\n"
                + "TX queue: 200 messages\nRX queue: 200 messages (inherited from TX)\n"
                + "Connection string: tcp://[::1]:5760/?reconnect=3000&tx_queue=200",
            description
        );
    }

    [Fact]
    public void TcpEndpoint_DescriptionMatchesDocumentedExample()
    {
        var config = new TcpClientProtocolPortConfig(
            new Uri("tcp://192.168.1.10:5760?reconnect=3000&tx_queue=200")
        );

        Assert.Equal(
            "Transport: TCP client\nEndpoint: tcp-client-1\nLocal address: 192.168.1.20:50123\n"
                + "Remote address: 192.168.1.10:5760\nProtocols: None\nIdle timeout: 3 s\n"
                + "TX queue: 200 messages\nRX queue: 200 messages (inherited from TX)\n"
                + "Connection string: tcp://192.168.1.10:5760/?reconnect=3000&tx_queue=200",
            ProtocolConnectionDisplay.NetworkEndpointDescription(
                "TCP client",
                "tcp-client-1",
                config,
                [],
                new IPEndPoint(IPAddress.Parse("192.168.1.20"), 50123),
                new IPEndPoint(IPAddress.Parse("192.168.1.10"), 5760)
            )
        );
    }

    [Theory]
    [InlineData("", "5 s")]
    [InlineData("?reconnect=-2", "Disabled")]
    [InlineData("?reconnect=-1", "Disabled")]
    [InlineData("?reconnect=0", "Disabled")]
    [InlineData("?reconnect=1234", "1.234 s")]
    public void NetworkEndpoint_DescribesIdleTimeoutAndMissingAddresses(
        string query,
        string timeout
    )
    {
        var config = new ProtocolPortConfig(new Uri($"tcp://127.0.0.1:5760{query}"));
        var description = ProtocolConnectionDisplay.NetworkEndpointDescription(
            "TCP",
            "unconnected",
            config,
            [],
            null,
            null
        );

        Assert.Equal(
            "Transport: TCP\nEndpoint: unconnected\nLocal address: Not bound\n"
                + $"Remote address: Not connected\nProtocols: None\nIdle timeout: {timeout}\n"
                + "TX queue: 100 messages\nRX queue: 100 messages\n"
                + $"Connection string: {config}",
            description
        );
    }

    [Theory]
    [InlineData(
        "tx_queue=17&tx_drop=true&rx_drop=false&rx_delay=12",
        "TX queue: 17 messages\nRX queue: 17 messages (inherited from TX)\n"
            + "TX queue overflow: Drop oldest messages\nRX queue overflow: Wait for space\n"
            + "Empty read delay: 12 ms\n"
    )]
    [InlineData(
        "tx_queue=0&rx_queue=-7&tx_drop=true&rx_drop=false&rx_delay=12",
        "TX queue: Unbounded\nRX queue: Unbounded\n"
            + "TX queue overflow: Not applicable (unbounded queue)\n"
            + "RX queue overflow: Not applicable (unbounded queue)\nEmpty read delay: 12 ms\n"
    )]
    public void NetworkEndpoint_DescribesEffectiveQueuesAndExplicitOptions(
        string query,
        string queues
    )
    {
        var config = new UdpProtocolPortConfig(new Uri($"udp://127.0.0.1:14550?{query}"));
        var description = ProtocolConnectionDisplay.NetworkEndpointDescription(
            "UDP",
            "queue-test",
            config,
            [],
            null,
            new IPEndPoint(IPAddress.Loopback, 14551)
        );

        Assert.Equal(
            "Transport: UDP\nEndpoint: queue-test\nLocal address: Not bound\n"
                + "Remote address: 127.0.0.1:14551\nProtocols: None\nIdle timeout: 5 s\n"
                + queues
                + $"Connection string: {config}",
            description
        );
    }

    [Theory]
    [InlineData(false, "System.IO.Ports")]
    [InlineData(true, "RJCP.IO.Ports")]
    public void SerialEndpoint_DescribesBackendAndEffectiveDefaults(
        bool alternative,
        string implementation
    )
    {
        var config = new SerialProtocolPortConfig(new Uri("serial:COM3"));
        var description = ProtocolConnectionDisplay.SerialEndpointDescription(
            "serial-peer",
            config,
            [],
            alternative
        );

        Assert.Equal(
            $"Transport: Serial\nEndpoint: serial-peer\nImplementation: {implementation}\n"
                + "Device: COM3\nBaud rate: 115200 bit/s\nData bits: 8\nParity: None\nStop bits: 1\n"
                + "Protocols: None\nTX queue: 100 messages\nRX queue: 100 messages\n"
                + $"Connection string: {config}",
            description
        );
    }

    [Theory]
    [InlineData(false, "ru-RU")]
    [InlineData(true, "ru-RU")]
    [InlineData(false, "en-US")]
    [InlineData(true, "en-US")]
    public void SerialEndpoint_FormatsFullSettingsInvariantly(bool alternative, string cultureName)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            var config = new SerialProtocolPortConfig(
                new Uri(
                    "serial:///dev/ttyUSB0?br=9600&data_bits=7&parity=Even&stop=OnePointFive"
                        + "&tx_queue=20&rx_queue=30&tx_drop=true&rx_drop=false&rx_delay=7"
                        + "&tx_size=4096&rx_size=8192&tx_timout=-1&rx_timout=1500&reconnect=1234"
                )
            );
            var description = ProtocolConnectionDisplay.SerialEndpointDescription(
                "serial-peer",
                config,
                [
                    new ProtocolInfo("zeta", "Zeta protocol"),
                    new ProtocolInfo("alpha", "Alpha protocol"),
                ],
                alternative
            );

            Assert.Equal(
                "Transport: Serial\nEndpoint: serial-peer\n"
                    + $"Implementation: {(alternative ? "RJCP.IO.Ports" : "System.IO.Ports")}\n"
                    + "Device: /dev/ttyUSB0\nBaud rate: 9600 bit/s\nData bits: 7\nParity: Even\nStop bits: 1.5\n"
                    + "Protocols: Alpha protocol, Zeta protocol\nTX queue: 20 messages\nRX queue: 30 messages\n"
                    + "TX queue overflow: Drop oldest messages\nRX queue overflow: Wait for space\nEmpty read delay: 7 ms\n"
                    + "TX buffer size: 4096 bytes\nRX buffer size: 8192 bytes\nTX timeout: Infinite\nRX timeout: 1.5 s\n"
                    + $"Connection string: {config}",
                description
            );
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Theory]
    [InlineData(false, 2, "0")]
    [InlineData(true, 1, "1 (configured as None)")]
    public void SerialEndpoint_UsesActualBackendForStopBitsMapping(
        bool alternative,
        int version,
        string stopBits
    )
    {
        var config = new SerialProtocolPortConfig(
            new Uri($"serial:COM3?stop=None&version={version}")
        );
        var description = ProtocolConnectionDisplay.SerialEndpointDescription(
            "serial-peer",
            config,
            [],
            alternative
        );

        Assert.Contains($"Stop bits: {stopBits}", description.Split('\n'));
        Assert.Equal($"Connection string: {config}", description.Split('\n')[^1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SerialEndpoint_FailedInitializationKeepsMetadataSnapshot(bool alternative)
    {
        var feature = new CapturingFeature();
        var context = Context(feature);
        var config = new SerialProtocolPortConfig(
            new Uri("serial:///__asv_io_missing__/port?br=9600")
        )
        {
            WriteTimeout = 0,
            Version = alternative ? 1U : 2U,
        };
        var connectionString = config.ToString();

        Assert.ThrowsAny<Exception>(() =>
        {
            if (alternative)
            {
                using var endpoint = new AlternativeSerialProtocolEndpoint(
                    "serial-peer",
                    config,
                    [],
                    context,
                    new Statistic()
                );
            }
            else
            {
                using var endpoint = new SerialProtocolEndpoint(
                    "serial-peer",
                    config,
                    [],
                    context,
                    new Statistic()
                );
            }
        });
        var captured = Assert.IsAssignableFrom<ProtocolEndpoint>(feature.Connection);
        await captured.Shutdown.WaitAsync(TestContext.Current.CancellationToken);
        var description = captured.Description;
        Assert.Equal("serial-peer", feature.DescriptionAtRegistration);
        Assert.True(captured.IsDisposed);
        Assert.Equal("port", captured.DisplayName);
        Assert.Equal(
            "Transport: Serial\nEndpoint: serial-peer\n"
                + $"Implementation: {(alternative ? "RJCP.IO.Ports" : "System.IO.Ports")}\n"
                + "Device: /__asv_io_missing__/port\nBaud rate: 9600 bit/s\nData bits: 8\nParity: None\nStop bits: 1\n"
                + "Protocols: None\nTX queue: 100 messages\nRX queue: 100 messages\nTX timeout: 0 s\n"
                + $"Connection string: {connectionString}",
            description
        );
        config.PortName = "changed-port";
        config.BoundRate = 38400;
        config.TxQueueSize = 10;

        Assert.Equal(description, captured.Description);
        Assert.Equal("port", captured.DisplayName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NetworkEndpoint_MetadataFailureDisposesParsersAndPreservesSocketOwnership(
        bool udp
    )
    {
        using var socket = new Socket(
            AddressFamily.InterNetwork,
            udp ? SocketType.Dgram : SocketType.Stream,
            udp ? ProtocolType.Udp : ProtocolType.Tcp
        );
        var config = new ProtocolPortConfig(
            new Uri(udp ? "udp://127.0.0.1:14550" : "tcp://127.0.0.1:5760")
        )
        {
            Port = -2,
        };
        var parser = new MetadataParser("test", "Test protocol");
        var feature = new CapturingFeature();
        var context = Context(feature);

        Assert.ThrowsAny<ArgumentException>(() =>
        {
            if (udp)
            {
                using var endpoint = new UdpSocketProtocolEndpoint(
                    socket,
                    new IPEndPoint(IPAddress.Loopback, 14551),
                    "invalid",
                    config,
                    [parser],
                    context,
                    new Statistic()
                );
            }
            else
            {
                using var endpoint = new TcpClientSocketProtocolEndpoint(
                    socket,
                    "invalid",
                    config,
                    [parser],
                    context,
                    new Statistic()
                );
            }
        });

        var captured = Assert.IsAssignableFrom<ProtocolEndpoint>(feature.Connection);
        await captured.Shutdown.WaitAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, parser.Disposals);
        if (udp)
        {
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            Assert.NotEqual(0, Assert.IsType<IPEndPoint>(socket.LocalEndPoint).Port);
        }
        else
        {
            Assert.Throws<ObjectDisposedException>(() =>
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0))
            );
        }
    }

    [Fact]
    public async Task CustomTcpEndpoint_PreservesProtectedConstructorAndGenericTransportLabel()
    {
        using var socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp
        );
        var config = new TcpServerProtocolPortConfig(new Uri("tcps://127.0.0.1:5760"));
        await using var endpoint = new CustomTcpEndpoint(socket, config);

        Assert.StartsWith("Transport: TCP\nEndpoint: custom-tcp\n", endpoint.Description);
        Assert.Contains("Remote address: Not connected", endpoint.Description.Split('\n'));
        Assert.Equal("custom-tcp", endpoint.DisplayName);
        Assert.Equal($"Connection string: {config}", endpoint.Description.Split('\n')[^1]);
    }

    [Fact]
    public async Task CustomProtocolEndpoint_PreservesIdFallback()
    {
        await using var endpoint = new CustomEndpoint();

        Assert.Equal("custom-endpoint", endpoint.Description);
        Assert.Equal("custom-endpoint", endpoint.DisplayName);
        await endpoint.DisposeAsync();
        Assert.Equal("custom-endpoint", endpoint.Description);
    }

    [Fact]
    public async Task VirtualEndpoint_KeepsStructuredDescriptionAfterDisposal()
    {
        await using var endpoint = new VirtualPort(
            "simulation",
            Context(),
            _ => true,
            new Statistic()
        );
        const string expected =
            "Transport: Virtual\nEndpoint: simulation\nConnection string: virtual://simulation";

        Assert.Equal(expected, endpoint.Description);
        Assert.Equal("simulation", endpoint.DisplayName);
        await endpoint.DisposeAsync();
        Assert.Equal(expected, endpoint.Description);
    }

    private static IProtocolContext Context(IProtocolFeature? feature = null)
    {
        var protocol = Protocol.Create(builder =>
        {
            builder.SetTimeProvider(new FakeTimeProvider());
            if (feature != null)
            {
                builder.Features.Register(feature);
            }
        });
        return (IProtocolContext)protocol;
    }

    private sealed class CustomTcpEndpoint(Socket socket, ProtocolPortConfig config)
        : TcpSocketProtocolEndpoint(
            socket,
            "custom-tcp",
            config,
            [],
            Context(),
            new Statistic()
        ) { }

    private sealed class CustomEndpoint()
        : ProtocolEndpoint(
            "custom-endpoint",
            new ProtocolPortConfig(new Uri("custom://localhost")),
            [],
            Context(),
            null,
            false
        )
    {
        protected override int GetAvailableBytesToRead() => throw new InvalidOperationException();

        protected override ValueTask<int> InternalRead(
            Memory<byte> memory,
            CancellationToken cancel
        ) => throw new InvalidOperationException();

        protected override ValueTask<int> InternalWrite(
            ReadOnlyMemory<byte> memory,
            CancellationToken cancel
        ) => throw new InvalidOperationException();
    }

    private sealed class MetadataParser(string id, string name) : IProtocolParser
    {
        private readonly Subject<IProtocolMessage> _messages = new();
        private readonly Subject<Exception> _errors = new();

        public int Disposals { get; private set; }
        public IStatistic Statistic { get; } = new Statistic();
        public ProtocolInfo Info { get; } = new(id, name);
        public ProtocolTags Tags { get; } = [];
        public Observable<IProtocolMessage> OnMessage => _messages;
        public Observable<Exception> OnError => _errors;

        public bool Push(byte data) => false;

        public void Reset() { }

        public void Dispose()
        {
            Disposals++;
            _messages.Dispose();
            _errors.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CapturingFeature : IProtocolFeature
    {
        public IProtocolConnection? Connection { get; private set; }
        public string? DescriptionAtRegistration { get; private set; }
        public string Name => nameof(CapturingFeature);
        public string Description => Name;
        public string Id => Name;
        public int Priority => 0;

        public ValueTask<IProtocolMessage?> ProcessTx(
            IProtocolMessage message,
            IProtocolConnection connection,
            CancellationToken cancel
        ) => ValueTask.FromResult<IProtocolMessage?>(message);

        public ValueTask<IProtocolMessage?> ProcessRx(
            IProtocolMessage message,
            IProtocolConnection connection,
            CancellationToken cancel
        ) => ValueTask.FromResult<IProtocolMessage?>(message);

        public void Register(IProtocolConnection connection)
        {
            Connection = connection;
            DescriptionAtRegistration = connection.Description;
        }

        public void Unregister(IProtocolConnection connection) { }
    }
}
