using System.Buffers;
using Asv.IO;
using ConsoleAppFramework;
using R3;

namespace Asv.Common.Shell;

public class TcpTest
{
    /// <summary>
    /// Command test tcp connection
    /// </summary>
    [Command("tcp-test")]
    public async Task<int> Run()
    {
        var client = PipePort.Create("tcp://127.0.0.1:7341", new ProtocolCore());
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
        return 0;
    }
}