using System;
using System.Threading.Tasks;
using Asv.Cfg.Test;
using Asv.IO;
using JetBrains.Annotations;
using R3;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test.Connection.Port.Impl;

[TestSubject(typeof(UdpProtocolPortConfig))]
public class UdpProtocolPortConfigTest(ITestOutputHelper output)
{
    [Fact(Skip = "Only for manual testing")]
    //[Fact]
    public async Task UpdConnectionTest()
    {
        var testLogFactory = new TestLoggerFactory(output, TimeProvider.System, "CLIENT");
        var protocol = IO.Protocol.Create(_ =>
        {
            _.Features.RegisterBroadcastAllFeature();
            _.SetDefaultMetrics();
            _.SetLog(testLogFactory);
            _.Protocols.RegisterExampleProtocol();
        });
        var serverRouter = protocol.CreateRouter("Server");
        var client1Router = protocol.CreateRouter("Client1");
        var client2Router = protocol.CreateRouter("Client2");

        var serverPort = serverRouter.AddPort("udp://127.0.0.1:5760");
        var client1Port = client1Router.AddPort("udp://127.0.0.1:5761?remote=127.0.0.1:5760&reconnect=0");
        var client2Port = client2Router.AddPort("udp://127.0.0.1:5762?remote=127.0.0.1:5760&reconnect=0");
        await serverPort.Status.FirstAsync(x => x == ProtocolPortStatus.Connected);
        await client1Port.Status.FirstAsync(x => x == ProtocolPortStatus.Connected);
        await client2Port.Status.FirstAsync(x => x == ProtocolPortStatus.Connected);

        var tcs = new TaskCompletionSource();
        var rcvCnt = 0;
        serverRouter.OnRxMessage.Subscribe(x =>
        {
            rcvCnt++;
            if (rcvCnt == 10)
            {
                tcs.SetResult();
            }
        });
        
        await Task.Delay(1000);
        for (int i = 0; i < 5; i++)
        {
            await client1Router.Send(new ExampleMessage1());
            await client2Router.Send(new ExampleMessage2());
            await Task.Delay(100);
        }
        
         
        await tcs.Task;
        
    }
}