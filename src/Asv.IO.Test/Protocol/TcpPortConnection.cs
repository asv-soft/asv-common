using System;
using System.Threading.Tasks;
using R3;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test;

public class TcpPortConnectionTest(ITestOutputHelper output)
{
    //[Fact] // this code is for manual testing only
    public async void TcpPortReconnectionTest()
    {
        
        
        var protocol = Protocol.Create(builder =>
        {
            builder.Protocols.RegisterExampleProtocol();
            builder.PortTypes.RegisterTcpClientPort();
        });
        var router = protocol.CreateRouter("TEST");
        var port = router.AddPort("tcp://127.0.0.1:5762");
        router.OnRxMessage.Subscribe(x =>
        {
            output.WriteLine(x.ToString());
        });
        router.OnTxMessage.Subscribe(x =>
        {
            output.WriteLine(x.ToString());
        });
        port.EndpointAdded.Subscribe(x =>
        {
            output.WriteLine($"Endpoint added: {x}");
        });
        port.EndpointRemoved.Subscribe(x =>
        {
            output.WriteLine($"Endpoint removed: {x}");
        });
        port.Status.Subscribe(x =>
        {
            output.WriteLine($"Port status: {x}");
        });
        while (true)
        {
            await Task.Delay(1000);
        }
        
    }
}