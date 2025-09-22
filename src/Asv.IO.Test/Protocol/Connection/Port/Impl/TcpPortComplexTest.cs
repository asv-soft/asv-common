using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Asv.Cfg.Test;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using R3;
using TimeProviderExtensions;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test.Connection.Port.Impl;

[Collection("Sequential")]
[TestSubject(typeof(ProtocolRouter))]
public class TcpPortComplexTest
{
    private readonly ManualTimeProvider _timer;
    private readonly TestLoggerFactory _logFactory;
    private readonly IProtocolFactory _protocol;
    private readonly IProtocolRouter _serverRouter;
    private readonly IProtocolRouter _clientRouter;
    private readonly ILogger _logger;

    public TcpPortComplexTest(ITestOutputHelper logger)
    {
        _timer = new ManualTimeProvider();
        _logFactory = new TestLoggerFactory(logger, TimeProvider.System, "ROUTER");
        _logger = _logFactory.CreateLogger("Test");
        _protocol = IO.Protocol.Create(builder =>
        {
            builder.SetLog(_logFactory);
            builder.Protocols.RegisterExampleProtocol();
            builder.Features.RegisterBroadcastAllFeature();
            builder.Formatters.RegisterJsonFormatter();
        });
        _serverRouter = _protocol.CreateRouter("Server");
        _clientRouter = _protocol.CreateRouter("Client");
    }

    [Theory(Skip = "This test can be performed only on a local machine.")]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task TcpPort_SendAndRecvMessages_Success(int messagesCount)
    {
        // Arrange
        var clientPort = _clientRouter.AddTcpServerPort(x =>
        {
            x.Host = "127.0.0.1";
            x.Port = 65500;
        });

        await clientPort.Status.FirstAsync(x => x == ProtocolPortStatus.Connected);

        var serverPort = _serverRouter.AddTcpClientPort(x =>
        {
            x.Host = "127.0.0.1";
            x.Port = 65500;
        });
        await serverPort.Status.FirstAsync(x => x == ProtocolPortStatus.Connected);

        // Act
        var tcs = new TaskCompletionSource();
        var cnt = 0;
        serverPort.OnRxMessage.Subscribe(x =>
        {
            cnt++;
            if (cnt % 100 == 0)
            {
                _logger.LogInformation($"Server received {cnt} messages");
                _serverRouter.Statistic.PrintRx(_logger);
                _serverRouter.Statistic.PrintTx(_logger);
                _serverRouter.Statistic.PrintParsed(_logger);
            }
            if (cnt >= messagesCount)
            {
                tcs.SetResult();
            }
        });

        new Thread(async void () =>
        {
            try
            {
                var index = 0;
                while (true)
                {
                    index++;
                    await clientPort.Send(new ExampleMessage1 { Value1 = 0 });

                    if (index % 100 == 0)
                    {
                        _logger.LogInformation($"Client send {index} messages");
                        _clientRouter.Statistic.PrintRx(_logger);
                        _clientRouter.Statistic.PrintTx(_logger);
                        _clientRouter.Statistic.PrintParsed(_logger);
                    }
                    if (index >= messagesCount)
                    {
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        }).Start();

        await tcs.Task;

        // Assert
        Assert.Equal(_clientRouter.Statistic.TxMessages, (uint)messagesCount);
        Assert.Equal(_serverRouter.Statistic.RxMessages, (uint)messagesCount);
    }

    [Theory(Skip = "This test can be performed only on a local machine.")]
    [InlineData("tcp://127.0.0.1:5650")]
    [InlineData("tcp://127.0.0.1:5652?srv=true")]
    [InlineData("udp://127.0.0.1:5651")]
    [InlineData("tcps://127.0.0.1:5651")]
    [InlineData("serial://127.0.0.1:5651")]
    public void Router_AddPortWithValidConnString_Success(string connectionString)
    {
        try
        {
            _clientRouter.AddPort(connectionString);
        }
        catch (Exception ex)
        {
            Assert.False(true, $"{ex}");
            throw;
        }
        finally
        {
            _clientRouter.Dispose();
        }
    }

    [Theory(Skip = "This test can be performed only on a local machine.")]
    [InlineData("tcp//127.0.0.1:5650")]
    [InlineData("127.0.0:561")]
    [InlineData("thisIsABadValue")]
    public void Router_AddPortWithInValidConnStringUriFormatException_Failure(
        string connectionString
    )
    {
        _clientRouter.PortAdded.Subscribe(_ => Assert.False(true));
        Assert.Throws<UriFormatException>(() =>
        {
            _clientRouter.AddPort(connectionString);
        });
    }

    [Theory(Skip = "This test can be performed only on a local machine.")]
    [InlineData("tcp:/127.0.0.15652?srv=true")]
    [InlineData("tcp:/127.0.0.15652?srvtru")]
    public void Router_AddPortWithInValidConnStringArgumentOutOfRange_Failure(
        string connectionString
    )
    {
        _clientRouter.PortAdded.Subscribe(_ => Assert.False(true));
        Assert.Throws<ArgumentOutOfRangeException>(() => _clientRouter.AddPort(connectionString));
    }

    [Theory(Skip = "This test can be performed only on a local machine.")]
    [InlineData("udp://1270.0:5651")]
    public void Router_AddPortWithInValidConnStringNetSocketException_Failure(
        string connectionString
    )
    {
        _clientRouter.PortAdded.Subscribe(_ => Assert.False(true));
        Assert.Throws<SocketException>(() => _clientRouter.AddPort(connectionString));
    }

    [Theory(Skip = "This test can be performed only on a local machine.")]
    [InlineData("p://127.0.0:5651")]
    public void Router_AddPortWithInValidConnStringInvalidOperationException_Failure(
        string connectionString
    )
    {
        _clientRouter.PortAdded.Subscribe(_ => Assert.False(true));
        Assert.Throws<InvalidOperationException>(() => _clientRouter.AddPort(connectionString));
    }

    //[Fact]
    [Fact(Skip = "This test can be performed only on a local machine.")]
    public void Router_RecreatePortWithAddAndRemove_Success()
    {
        const string validConnString = "tcp://127.0.0.1:5650";
        _clientRouter.PortAdded.Subscribe(_ => Assert.True(true));
        var port = _clientRouter.AddPort(validConnString);
        _clientRouter.PortRemoved.Subscribe(_ => Assert.Equal(port, _));
        _clientRouter.RemovePort(port);
        var port1 = _clientRouter.AddPort(validConnString);
        Assert.NotNull(port1);
    }

    //[Fact]
    [Fact(Skip = "This test can be performed only on a local machine.")]
    public void Router_SetConnectionId_Success()
    {
        const string validConnString = "tcp://127.0.0.1:5650";
        var port = _clientRouter.AddPort(validConnString);
        _serverRouter.AddPort("tcps://127.0.0.1:5650");
        _clientRouter.OnRxMessage.Subscribe();
    }

    //[Fact]
    [Fact(Skip = "This test can be performed only on a local machine.")]
    public void Router_SetPortId_Success()
    {
        const string validConnString = "tcp://127.0.0.1:5650";
        var port = _clientRouter.AddPort(validConnString);
        _serverRouter.AddPort("tcps://127.0.0.1:5650");
    }
}
