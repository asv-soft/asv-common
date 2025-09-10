using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using R3;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test
{
    public class TelnetStreamTest
    {
        private readonly ITestOutputHelper _output;

        public TelnetStreamTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact(Skip = "Manual test")]
        public async Task ReadWriteMessageTest()
        {
            var message1 = "Ping";
            var message2 = "Pong";

            using var port1 = new VirtualDataStream("port1");
            using var port2 = new VirtualDataStream("port2");
            port2.TxPipe.Subscribe(port1.RxPipe);
            port1.TxPipe.Subscribe(port2.RxPipe);

            using var strm1 = new TelnetStream(port1, Encoding.ASCII);
            using var strm2 = new TelnetStream(port2, Encoding.ASCII);

            strm1
                .OnReceive.Where(_ => _.Equals(message1))
                .Subscribe(_ => strm1.Send(message2, CancellationToken.None).Wait(1000));

            var result = await strm2.RequestText(message1, 3000, CancellationToken.None);
            Assert.Equal(message2, result);
        }

        [Fact]
        public async Task BufferOverflow()
        {
            using var port1 = new VirtualDataStream("port1");
            using var port2 = new VirtualDataStream("port2");
            port2.TxPipe.Subscribe(port1.RxPipe);
            port1.TxPipe.Subscribe(port2.RxPipe);
            using var strm1 = new TelnetStream(port1, Encoding.ASCII);
            using var strm2 = new TelnetStream(port2, Encoding.ASCII, 10);

            var tcs = new TaskCompletionSource<Exception>();
            using var c1 = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var r1 = c1.Token.Register(() => tcs.TrySetCanceled());

            using var a = strm2
                .OnError.Take(1)
                .Subscribe(_ =>
                {
                    _output.WriteLine(_.Message);
                    tcs.SetResult(_);
                });

            await strm1.Send("1234567890", CancellationToken.None);

            await tcs.Task;

            Assert.Throws<InternalBufferOverflowException>(new Action(() => throw tcs.Task.Result));
        }
    }
}
