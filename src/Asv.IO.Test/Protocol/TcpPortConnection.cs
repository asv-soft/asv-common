using System;
using System.Threading;
using System.Threading.Tasks;
using Asv.Cfg.Test;
using R3;
using Xunit;
using Xunit.Abstractions;
using ZLogger;

namespace Asv.IO.Test;

public class TcpPortConnectionTest(ITestOutputHelper output)
{
    // [Fact] // this code is for manual testing only
    public async void TcpPortReconnectionTest()
    {
       
        
        var logFactory1 = new TestLoggerFactory(output, TimeProvider.System, "CLIENT");
        var protocol1 = Protocol.Create(builder =>
        {
            builder.SetLog(logFactory1);
            builder.Protocols.RegisterExampleProtocol();
            builder.PortTypes.RegisterTcpClientPort();
        });
        var router1 = protocol1.CreateRouter("1");
        var port1 = router1.AddPort("tcp://127.0.0.1:5762?reconnect=-1");
        var logger1 = logFactory1.CreateLogger("Logger1");
        
        var logFactory2 = new TestLoggerFactory(output, TimeProvider.System, "SERVER");
        var protocol2 = Protocol.Create(builder =>
        {
            builder.SetLog(logFactory2);
            builder.Protocols.RegisterExampleProtocol();
            builder.PortTypes.RegisterTcpServerPort();
        });
        var rxIndex = 0;
        var router2 = protocol2.CreateRouter("2");
        var port2 = router2.AddPort("tcps://127.0.0.1:5762?reconnect=-1");
        var logger2 = logFactory2.CreateLogger("Logger2");
        router2.OnRxMessage.Subscribe(x =>
        {
            logger2.ZLogTrace($"RX MESSAGE {rxIndex++}");
        }, ex => logger2.ZLogError(ex, $"Error in RX message handler"), _ => { });
        

        var txIndex = 0;
        var cancel = new CancellationTokenSource();
        await Task.Factory.StartNew(async () =>
        {
            while (cancel.IsCancellationRequested == false)
            {
                await Task.Delay(1000, cancel.Token);
                logger1.ZLogTrace($"TX MESSAGE {txIndex++}");
                await router1.Send(new ExampleMessage3{}, cancel.Token);
            }
        }, cancel.Token);
        
      
        
        /*output.WriteLine("==========BEGIN============");
        await Task.Delay(10_000);
        port1.Disable();
        output.WriteLine("==========DISABLE============");
        await Task.Delay(10_000);
        output.WriteLine("==========ENABLE============");
        port1.Enable();
        await Task.Delay(10_000);
        output.WriteLine("==========END============");*/
        await Task.Delay(10_000);

    }
}