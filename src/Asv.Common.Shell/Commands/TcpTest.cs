using System.Reflection;
using Asv.IO;
using BenchmarkDotNet.Running;
using ConsoleAppFramework;
using Microsoft.Extensions.Logging;
using R3;
using ZLogger;
using Exception = System.Exception;

namespace Asv.Common.Shell;

public class TcpTest
{
    [Command("b-test")]
    public void Benchmark()
    {
        BenchmarkRunner.Run<SwitchVsDictionary>();
    }

    // private readonly ManualTimeProvider _timer;
    // private readonly TestLoggerFactory _logFactory;
    private IProtocolFactory _protocol;
    private IProtocolRouter _serverRouter;
    private IProtocolRouter _clientRouter;
    private ILogger _logger;
    private ILoggerFactory _factory;
    private string _server;
    private string _client;

    /// <summary>
    /// Command test tcp connection
    /// <param name="server">-s, Server connection string </param>
    /// <param name="client">-c, Client connection string </param>
    /// </summary>
    [Command("tcp-test")]
    public async Task<int> Run(
        string server = "tcps://127.0.0.1:7341",
        string client = "tcp://127.0.0.1:7341"
    /*string server = "serial:COM11?br=57600",
    string client = "serial:COM45?br=57600"*/
    )
    {
        var loggerFactory = ConsoleAppHelper.CreateDefaultLog();
        _server = server;
        _client = client;
        _factory = loggerFactory;
        _protocol = Protocol.Create(builder =>
        {
            builder.SetLog(_factory);
            builder.Protocols.RegisterExampleProtocol();
            builder.Features.RegisterBroadcastAllFeature();
            builder.Formatters.RegisterJsonFormatter();
        });
        _serverRouter = _protocol.CreateRouter("Server");

        _clientRouter = _protocol.CreateRouter("Client");
        _clientRouter.AddPort(_client);

        var logger = loggerFactory.CreateLogger<TcpTest>();
        _logger = logger;
        Assembly.GetExecutingAssembly().PrintWelcomeToLog(logger);

        var result = await CheckResult(Router_RecreatePortWithAddAndRemove_Success());
        result = await CheckResult(Router_ServerAndClientExchangePackets_Success());
        result = await CheckResult(
            Router_AddPortWithInValidConnStringInvalidOperationException_Failure()
        );
        return result;
    }

    private async Task<int> Router_ServerAndClientExchangePackets_Success()
    {
        const int messagesCount = 1000;
        var serverPort = _serverRouter.AddPort(_server);
        var clientPort = _clientRouter.AddPort(_client);
        var config = clientPort.Config.AsUri();

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
        var result = 0;
        new Thread(async void () =>
        {
            try
            {
                var index = 0;
                while (result == 0)
                {
                    index++;
                    await clientPort.Send(new ExampleMessage1 { Value1 = 0 });
                    Thread.Sleep(1);
                    if (index % 100 == 0)
                    {
                        _logger.LogInformation($"Client send {index} messages");
                        _clientRouter.Statistic.PrintRx(_logger);
                        _clientRouter.Statistic.PrintTx(_logger);
                        _clientRouter.Statistic.PrintParsed(_logger);
                    }

                    if (index >= messagesCount)
                    {
                        result = 1;
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

        serverPort.Dispose();
        clientPort.Dispose();
        return 0;
    }

    private async Task<int> CheckResult(Task<int> func)
    {
        var result = await func;
        switch (result)
        {
            case 1:
                _logger.ZLogError($"Test is failed");
                break;
            case 0:
                _logger.LogInformation($"Success");
                break;
        }

        return result;
    }

    private Task<int> Router_RecreatePortWithAddAndRemove_Success()
    {
        const string validConnString = "tcp://127.0.0.1:5650";
        _clientRouter.PortAdded.Subscribe(_ => { });
        var port = _clientRouter.AddPort(validConnString);
        _clientRouter.PortRemoved.Subscribe(_ =>
        {
            if (port == _) { }
        });
        _clientRouter.RemovePort(port);
        var port1 = _clientRouter.AddPort(validConnString);

        return Task.FromResult(0);
    }

    private Task<int> Router_AddPortWithInValidConnStringInvalidOperationException_Failure()
    {
        var assertValue = 0;
        var connectionString = "p://127.0.0:5651";
        _clientRouter.PortAdded.Subscribe(_ => assertValue = 1);
        try
        {
            _clientRouter.AddPort(connectionString);
        }
        catch (InvalidOperationException)
        {
            assertValue = 0;
        }

        return Task.FromResult(assertValue);
    }
}
