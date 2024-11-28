using System;
using System.Threading;
using System.Threading.Tasks;
using Asv.Cfg.Test;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using R3;
using TimeProviderExtensions;
using Xunit;
using Xunit.Abstractions;
using Asv.IO;

namespace Asv.IO.Test;

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
        _logFactory = new TestLoggerFactory(logger,TimeProvider.System, "ROUTER");
        _logger = _logFactory.CreateLogger("Test");
        _protocol = IO.Protocol.Create(builder =>
        {
            builder.SetLog(_logFactory);
            builder.RegisterExampleProtocol();
            builder.EnableBroadcastAllMessages();
            builder.RegisterJsonFormatter();
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
        var serverPort = _serverRouter.AddTcpClientPort(x =>
        {
            x.Host = "127.0.0.1";
            x.Port = 7341;
        });

        var clientPort = _clientRouter.AddTcpServerPort(x =>
        {
            x.Host = "127.0.0.1";
            x.Port = 7341;
        });
        // Act

        await clientPort.Status.FirstAsync(x => x == ProtocolPortStatus.Connected);
        await serverPort.Status.FirstAsync(x => x == ProtocolPortStatus.Connected);
        
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
                    await clientPort.Send(new ExampleMessage1{ Value1 = 0});
                    
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
    }
}