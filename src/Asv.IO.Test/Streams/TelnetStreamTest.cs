using System;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Asv.IO.Test.Streams
{
    public class TelnetStreamTest
    {
        [Fact]
        public async Task ReadWriteMessageTest()
        {
            var message1 = "Ping";
            var message2 = "Pong";

            using var port1 = PortFactory.Create("tcp://127.0.0.1:55000", true);
            using var port2 = PortFactory.Create("tcp://127.0.0.1:55000?srv=true", true);
            using var strm1 = new TelnetStream(port1, Encoding.ASCII);
            using var strm2 = new TelnetStream(port2, Encoding.ASCII);
            while (port1.State.Value != PortState.Connected && port2.State.Value != PortState.Connected)
            {
                await Task.Delay(1000);
            }
            strm1.Where(_ => _.Equals(message1)).Subscribe(_ => strm1.Send(message2, CancellationToken.None).Wait(1000));

            var result = await strm2.RequestText(message1, 3000, CancellationToken.None);
            Assert.Equal(message2, result);
        }

        [Fact]
        public async Task BufferOverflow()
        {
            using var port1 = PortFactory.Create("tcp://127.0.0.1:55000", true);
            using var port2 = PortFactory.Create("tcp://127.0.0.1:55000?srv=true", true);
            using var strm1 = new TelnetStream(port1, Encoding.ASCII, 10);
            using var strm2 = new TelnetStream(port2, Encoding.ASCII, 10);
            while (port1.State.Value != PortState.Connected && port2.State.Value != PortState.Connected)
            {
                await Task.Delay(1000);
            }
 
            var tcs = new TaskCompletionSource<Exception>();
            
            using var a = strm2.OnError.FirstAsync().Subscribe(_=> tcs.SetResult(_));

            await strm1.Send("1234567890", CancellationToken.None);

            await tcs.Task;

            Assert.Throws<InternalBufferOverflowException>(new Action(() => throw tcs.Task.Result));


        }

    }
}