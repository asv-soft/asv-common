using System.Buffers;
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

    /// <summary>
    /// Command test tcp connection
    /// <param name="server">-s, Server connection string </param>
    /// <param name="client">-c, Client connection string </param>
    /// </summary>
    [Command("tcp-test")]
    public async Task<int> Run(
        /*string server = "tcps://127.0.0.1:7341?max_clients=10#protocols=example",
        string client = "tcp://127.0.0.1:7341#protocols=example"*/
        string server = "serial:COM11?br=57600",
        string client = "serial:COM45?br=57600"
    )
    {
        var loggerFactory = ConsoleAppHelper.CreateDefaultLog();
        var logger = loggerFactory.CreateLogger<TcpTest>();
        Assembly.GetExecutingAssembly().PrintWelcomeToLog(logger);
        const int messagesCount = 100;
        var protocol = Protocol.Create(builder =>
        {
            builder.SetLog(loggerFactory);
            builder.RegisterExampleProtocol();
            builder.EnableBroadcastFeature();
            builder.AddPrinterJson();
        });

        var serverRouter = protocol.CreateRouter("Server");
        var serverPort = serverRouter.AddPort(server);
        
        var clientRouter = protocol.CreateRouter("Client");
        var clientPort = clientRouter.AddPort(client);
        var config = clientPort.Config.AsUri();

        await clientPort.Status.FirstAsync(x => x == ProtocolPortStatus.Connected);
        await serverPort.Status.FirstAsync(x => x == ProtocolPortStatus.Connected);
        
        var tcs = new TaskCompletionSource();
        var cnt = 0;
        serverPort.OnRxMessage.Subscribe(x =>
        {
            cnt++;
            if (cnt % messagesCount == 0)
            {
                logger.LogInformation($"Received {cnt} messages");
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
                    if (index % messagesCount == 0)
                    {
                        logger.LogInformation($"Send {index} messages");
                        
                    }
                }
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        }).Start();
        
        
        await tcs.Task;

        Console.ReadLine();
        
        return 0;

        //var portFactory = new ProtocolPortFactory(core, parserFactory);

        /*var client = PipePort.Create("tcp://127.0.0.1:7341", new ProtocolCore());
        var server = PipePort.Create("tcp://127.0.0.1:7341?srv=true", new ProtocolCore());
        server.Enable();
        client.Enable();

        client.Status.Subscribe(x=>Console.WriteLine($"Client Status: {x:G}"));
        server.Status.Subscribe(x=>Console.WriteLine($"Server Status: {x:G}"));

        var data = new byte[4096*3];
        Random.Shared.NextBytes(data);

        await client.WaitConnected(TimeSpan.FromSeconds(10));
        await server.WaitConnected(TimeSpan.FromSeconds(10));

        new Thread(() =>
        {
            while (true)
            {
                foreach (var pipe in client.Pipes)
                {
                    pipe.Output.Write(data);
                    pipe.Output.FlushAsync();
                }
            }
        }).Start();

        new Thread(() =>
        {
            while (true)
            {
                foreach (var pipe in server.Pipes)
                {
                    while (true)
                    {
                        if (pipe.Input.TryRead(out var result))
                        {

                        }
                        else
                        {
                            Thread.Sleep(100);
                        }

                    }

                }
            }
        }).Start();


        ConsoleAppHelper.WaitCancelPressOrProcessExit();
        return 0;*/

    }
}