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

    [Fact]
    public async Task METHOD()
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
        var clientRouter = protocol.CreateRouter("Client");

        var server = serverRouter.AddPort("udp://127.0.0.1:5761");
        server.Enable();
        var client = clientRouter.AddPort("udp://127.0.0.1:5760?rhost=127.0.0.1&rport=5761");
        client.Enable();

        var tcs = new TaskCompletionSource();
        
        serverRouter.OnRxMessage.Subscribe(x =>
        {
            tcs.SetResult();
        });
        for (int i = 0; i < 10; i++)
        {
            await clientRouter.Send(new ExampleMessage1());
            await Task.Delay(100);
        }
        
         
        await tcs.Task;
        
    }
}