using System;
using System.Threading;
using System.Threading.Tasks;
using Asv.Cfg.Test;
using Asv.IO;
using AutoFixture;
using JetBrains.Annotations;
using R3;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test.Protocol.Connection.Virtual;

[TestSubject(typeof(VirtualPort))]
[TestSubject(typeof(VirtualConnection))]
public class VirtualPortTest
{
    private readonly ITestOutputHelper _output;

    public VirtualPortTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task VirtualConnection_SendServerToClient_Success(int count)
    {
        // Arrange
        
        var fixture = new Fixture();
        var link = IO.Protocol.Create(builder =>
        {
            builder.SetLog(new TestLoggerFactory(_output, TimeProvider.System,"ROUTER"));
            builder.Protocols.RegisterExampleProtocol();
            builder.Formatters.RegisterSimpleFormatter();
        }).CreateVirtualConnection();

        var sendArray1 = new ExampleMessage1[count];
        for (int i = 0; i < count; i++)
        {
            sendArray1[i] = fixture.Create<ExampleMessage1>();
        }
        var sendArray2 = new ExampleMessage2[count];
        for (int i = 0; i < count; i++)
        {
            sendArray2[i] = fixture.Create<ExampleMessage2>();
        }
        
        var actualRx1 = 0;
        var actualRx2 = 0;
        
        var tcs = new TaskCompletionSource();
        var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        cancel.Token.Register(()=>tcs.TrySetException(new TimeoutException()));
        
        // Act
        link.Client.OnRxMessage.Subscribe(x =>
        {
            switch (x)
            {
                case ExampleMessage1:
                    actualRx1++;
                    break;
                case ExampleMessage2:
                    actualRx2++;
                    break;
                default:
                    tcs.TrySetException(new Exception("Unknown message type"));
                    break;
            }

            if (actualRx1 == count && actualRx2 == count)
            {
                tcs.TrySetResult();
            }
        });
        foreach (var message1 in sendArray1)
        {
            await link.Server.Send(message1, cancel.Token);
        }
        foreach (var message2 in sendArray2)
        {
            await link.Server.Send(message2, cancel.Token);
        }
        
        // Assert
        await tcs.Task;
        
        Assert.Equal(count*2, (int)link.Server.Statistic.TxMessages);
        Assert.Equal(count*2, (int)link.Client.Statistic.RxMessages);
        
    }
}