using System.Buffers;
using Asv.IO;
using BenchmarkDotNet.Running;
using ConsoleAppFramework;
using Microsoft.Extensions.Logging;
using R3;
using ZLogger;

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
    /// <param name="logLevel">-v, Logging level </param>
    /// </summary>
    [Command("tcp-test")]
    public async Task<int> Run(
#if DEBUG
        LogLevel logLevel = LogLevel.Trace
#else
        LogLevel logLevel = LogLevel.Information
#endif
    )
    {


        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(logLevel);
            builder.AddZLoggerConsole(options =>
            {
                options.IncludeScopes = true;

                options.UsePlainTextFormatter(formatter =>
                {
                    formatter.SetPrefixFormatter($"{0:HH:mm:ss.fff} | ={1:short}= | {2,-40} ",
                        (in MessageTemplate template, in LogInfo info) =>
                            template.Format(info.Timestamp, info.LogLevel, info.Category));
                    formatter.SetExceptionFormatter((writer, ex) =>
                        Utf8StringInterpolation.Utf8String.Format(writer, $"{ex.Message}"));
                });
            });
        });
        var protocol = Protocol.Create(builder =>
        {
            builder.SetLog(loggerFactory);
            builder.RegisterExampleProtocol();
            
            builder.RegisterSerialPort();
            builder.RegisterTcpClientPort();
            builder.RegisterTcpServerPort();
            builder.RegisterUdpPort();
            
        });
        
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