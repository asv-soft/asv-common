using System.IO.Ports;
using Asv.IO;
using ConsoleAppFramework;
using DotNext.Threading.Tasks;
using R3;

namespace Asv.Common.Shell;

public class SerialCommand
{
    /// <summary>
    /// Serial port test
    /// </summary>
    /// <param name="portName">-p, port name</param>
    /// <returns></returns>
    [Command("serial")]
    public async Task<int> Serial(string portName)
    {
        var protocol = Protocol.Create(builder =>
        {
            builder.PortTypes.RegisterSerialPort();
            builder.Protocols.RegisterExampleProtocol();
        });
        var router = protocol.CreateRouter("ROUTER");
        var port = router.AddSerialPort(x =>
        {
            x.PortName = portName;
            x.BoundRate = 115200;
            x.DataBits = 8;
            x.Parity = Parity.None;
            x.StopBits = StopBits.One;
            x.Version = 2;
        });
        port.Enable();
        await port.Status.FirstOrDefaultAsync(x => x == ProtocolPortStatus.Connected);
        while (true)
        {
            await router.Send(new ExampleMessage1());
            await Task.Delay(1000);
        }

        return 0;
    }
}
