using System;
using System.Threading;
using System.Threading.Tasks;
using DeepEqual.Syntax;
using JetBrains.Annotations;
using Microsoft.Extensions.Time.Testing;
using R3;
using Xunit;

namespace Asv.IO.Test;

[TestSubject(typeof(ProtocolConnection))]
public class ProtocolConnectionTests
{
    private IProtocolRouter _serverRouter;
    private IProtocolRouter _clientRouter;

    private readonly FakeTimeProvider _timeProvider = new();

    public ProtocolConnectionTests()
    {
        var protocol = IO.Protocol.Create(_ =>
        {
            _.Features.RegisterBroadcastAllFeature();
            _.SetDefaultMetrics();
            _.SetTimeProvider(_timeProvider);
            _.Protocols.RegisterExampleProtocol();
        });
        _serverRouter = protocol.CreateRouter("Server");
        _clientRouter = protocol.CreateRouter("Client");

        _serverRouter.AddPort("tcps://127.0.0.1:5760");
        _clientRouter.AddPort("tcp://127.0.0.1:5760");
    }

    private class NewPacketMessageFormater : IProtocolMessageFormatter
    {
        public string Name { get; } = "TestPacketFormater";
        public int Order { get; }

        public bool CanPrint(IProtocolMessage message)
        {
            return message is ExampleMessage1;
        }

        public string Print(IProtocolMessage packet, PacketFormatting formatting)
        {
            if (CanPrint(packet))
            {
                return formatting switch
                {
                    PacketFormatting.Inline =>
                        $"TestPacketWithInlineFormating:{packet.Name} {packet.Protocol} {packet}",
                    PacketFormatting.Indented =>
                        $"TestPacketWithIndentedFormating:{packet.Name} {packet.Protocol} {packet}",
                    _ => $"TestPacketWithNoFormating:{packet.Name} {packet.Protocol} {packet}"
                };
            }

            throw new ArgumentOutOfRangeException();
        }
    }

    [Fact]
    public async Task Connection_StatisticSends_Success()
    {
        var maxcount = 100;
        var package = 0;
        _clientRouter.OnRxMessage.Subscribe(_ => { package++; });
        var tcs = new TaskCompletionSource();
        new Thread(async () =>
        {
            for (var i = 0; i < maxcount; i++)
            {
                await _serverRouter.Send(new ExampleMessage1());
                Thread.Sleep(1);
            }

            tcs.SetResult();
        }).Start();
        await tcs.Task;
        Assert.True(_clientRouter.Statistic.RxMessages == package);
        Assert.True(_serverRouter.Statistic.TxMessages == package);
    }

    [Fact]
    public void Connection_MessagePacketPrintInDifferentFormats_Success()
    {
        //Arrange
        var message = new ExampleMessage1()
        {
            Value1 = 100,
            Tags = new ProtocolTags(),
        };

        //Act

        //simple format
        var messageSimpleFormat = message.ToString();
        var messageSimplePrintedInline = _clientRouter.PrintMessage(message);
        Assert.Equal(messageSimpleFormat, messageSimplePrintedInline);

        //packet custom type formatter
        _clientRouter = IO.Protocol.Create(_ => { _.Formatters.Register(new NewPacketMessageFormater()); })
            .CreateRouter("Client");
        NewPacketMessageFormater formater = new();
        var message2 = new ExampleMessage2();
        Assert.Throws<ArgumentOutOfRangeException>(() => { formater.Print(message2, PacketFormatting.Indented); });
        var result = formater.Print(message, PacketFormatting.Inline);
        var clientResult = _clientRouter.PrintMessage(message);
        Assert.Equal(result, clientResult);
    }

    [Fact]
    public async Task Connection_InvokesObservableOnTransactMessage()
    {
        _clientRouter.OnTxMessage.Subscribe(_ => Assert.True(_.IsDeepEqual(new ExampleMessage1())));
        await _clientRouter.Send(new ExampleMessage1());
    }

    [Fact]
    public void Connection_RegisterSpecifiedFeature_Success()
    {
        IProtocolFeature feature = new BroadcastingFeature<ExampleMessage1>();
        _clientRouter = IO.Protocol.Create(_ => { _.Features.Register(feature); }).CreateRouter("Client");
        Assert.NotNull(_clientRouter);
    }

    [Fact]
    public void Connection_RegisterSpecifiedFeatureWithNullValue_Fail()
    {
        IProtocolFeature feature = null;
        Assert.Throws<ArgumentNullException>(() =>
        {
            _clientRouter = IO.Protocol.Create(_ => { _.Features.Register(feature); }).CreateRouter("Client");
        });
    }

}