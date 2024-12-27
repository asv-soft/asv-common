using System;
using System.Threading;
using System.Threading.Tasks;
using Asv.Cfg.Test;
using AutoFixture;
using JetBrains.Annotations;
using R3;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test.Connection.Virtual;

[TestSubject(typeof(VirtualPort))]
[TestSubject(typeof(VirtualConnection))]
public class VirtualConnectionTest
{
    private readonly ITestOutputHelper _output;

    public VirtualConnectionTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task VirtualConnection_SetClientToServerFilter_Success(int count)
    {
        // Arrange
        var fixture = new Fixture();
        var tcs = new TaskCompletionSource();
        var link = Protocol.Create(builder =>
        {
            builder.SetLog(new TestLoggerFactory(_output, TimeProvider.System, "ROUTER"));
            builder.Protocols.RegisterExampleProtocol();
        }).CreateVirtualConnection();
        
        var sendArray1 = new ExampleMessage1[count];
        for (var i = 0; i < count; i++)
        {
            sendArray1[i] = fixture.Create<ExampleMessage1>();
        }

        var sendArray2 = new ExampleMessage2[count];
        for (var i = 0; i < count; i++)
        {
            sendArray2[i] = fixture.Create<ExampleMessage2>();
        }

        var all = 0;
        link.SetClientToServerFilter(x=>
        {
            all++;
            return x is ExampleMessage1;
        });
        var msg1 = 0;
        var msg2 = 0;
        link.Server.RxFilterByType<ExampleMessage1>().Subscribe(x =>
        {
            msg1++;
            if (msg1 == count)
            {
                tcs.TrySetResult();
            }
        });
        link.Server.RxFilterByType<ExampleMessage2>().Subscribe(x =>
        {
            msg2++;
        });
        
        // Act
        foreach (var message1 in sendArray1)
        {
            await link.Client.Send(message1, CancellationToken.None);
        }
        foreach (var message2 in sendArray2)
        {
            await link.Client.Send(message2, CancellationToken.None);
        }

        await tcs.Task;
        
        // Assert
        
        Assert.Equal(count*2,all);
        Assert.Equal(count,msg1);
        Assert.Equal(0,msg2);
        
    }
    
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task VirtualConnection_SetServerToClientFilter_Success(int count)
    {
        // Arrange
        var fixture = new Fixture();
        var tcs = new TaskCompletionSource();
        var link = Protocol.Create(builder =>
        {
            builder.SetLog(new TestLoggerFactory(_output, TimeProvider.System, "ROUTER"));
            builder.Protocols.RegisterExampleProtocol();
        }).CreateVirtualConnection();
        
        var sendArray1 = new ExampleMessage1[count];
        for (var i = 0; i < count; i++)
        {
            sendArray1[i] = fixture.Create<ExampleMessage1>();
        }

        var sendArray2 = new ExampleMessage2[count];
        for (var i = 0; i < count; i++)
        {
            sendArray2[i] = fixture.Create<ExampleMessage2>();
        }

        var all = 0;
        link.SetServerToClientFilter(x=>
        {
            all++;
            return x is ExampleMessage1;
        });
        var msg1 = 0;
        var msg2 = 0;
        link.Client.OnRxMessage.FilterByType<ExampleMessage1>().Subscribe(x =>
        {
            msg1++;
            if (msg1 == count)
            {
                tcs.TrySetResult();
            }
        });
        link.Client.OnRxMessage.FilterByType<ExampleMessage2>().Subscribe(x =>
        {
            msg2++;
        });
        
        // Act
        foreach (var message1 in sendArray1)
        {
            await link.Server.Send(message1, CancellationToken.None);
        }
        foreach (var message2 in sendArray2)
        {
            await link.Server.Send(message2, CancellationToken.None);
        }

        
        
        // Assert
        
        Assert.Equal(count*2,all);
        Assert.Equal(count,msg1);
        Assert.Equal(0,msg2);
        
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
        var link = Protocol.Create(builder =>
        {
            builder.SetLog(new TestLoggerFactory(_output, TimeProvider.System, "ROUTER"));
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
        var cancel = new CancellationTokenSource();
        cancel.Token.Register(() => tcs.TrySetException(new TimeoutException()));

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
        Assert.Equal(count * 2, (int)link.Statistic.ParsedMessages);
        Assert.Equal(count * 2, (int)link.Server.Statistic.TxMessages);
        Assert.Equal(count * 2, (int)link.Client.Statistic.RxMessages);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task VirtualConnection_SendClientToServer_Success(int count)
    {
        // Arrange

        var fixture = new Fixture();
        var link = Protocol.Create(builder =>
        {
            builder.SetLog(new TestLoggerFactory(_output, TimeProvider.System, "ROUTER"));
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
        var cancel = new CancellationTokenSource();
        cancel.Token.Register(() => tcs.TrySetException(new TimeoutException()));

        // Act
        link.Server.OnRxMessage.Subscribe(x =>
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
            await link.Client.Send(message1, cancel.Token);
        }

        foreach (var message2 in sendArray2)
        {
            await link.Client.Send(message2, cancel.Token);
        }

        // Assert
        await tcs.Task;

        Assert.Equal(count * 2, (int)link.Statistic.ParsedMessages);
        Assert.Equal(count * 2, (int)link.Client.Statistic.TxMessages);
        Assert.Equal(count * 2, (int)link.Server.Statistic.RxMessages);
    }

    [Fact]
    public async Task Statistic_IncrementRXPackets_Success()
    {
        //Arrange
        uint count = 10;
        var link = Protocol.Create(builder =>
        {
            builder.SetLog(new TestLoggerFactory(_output, TimeProvider.System, "ROUTER"));
            builder.Protocols.RegisterExampleProtocol();
            builder.Formatters.RegisterSimpleFormatter();
        }).CreateVirtualConnection();
        var fixture = new Fixture();
        var sendArray1 = new ExampleMessage1[10];
        var tcs = new TaskCompletionSource();
        var cancel = new CancellationTokenSource();
        cancel.Token.Register(() => tcs.TrySetException(new TimeoutException()));
        for (int i = 0; i < 10; i++)
        {
            sendArray1[i] = fixture.Create<ExampleMessage1>();
        }

        var sendArray2 = new ExampleMessage2[10];
        for (int i = 0; i < count; i++)
        {
            sendArray2[i] = fixture.Create<ExampleMessage2>();
        }

        //Act
        foreach (var message1 in sendArray1)
        {
            await link.Server.Send(message1, cancel.Token);
        }

        foreach (var message2 in sendArray2)
        {
            await link.Client.Send(message2, cancel.Token);
        }

        //Assert
        Assert.Equal(count * 2, link.Statistic.RxMessages);
    }

    [Fact]
    public async Task Statistic_IncrementTXPackets_Success()
    {
        //Arrange
        uint count = 10;
        var link = Protocol.Create(builder =>
        {
            builder.SetLog(new TestLoggerFactory(_output, TimeProvider.System, "ROUTER"));
            builder.Protocols.RegisterExampleProtocol();
            builder.Formatters.RegisterSimpleFormatter();
        }).CreateVirtualConnection();
        var fixture = new Fixture();
        var sendArray1 = new ExampleMessage1[10];
        var tcs = new TaskCompletionSource();
        var cancel = new CancellationTokenSource();
        cancel.Token.Register(() => tcs.TrySetException(new TimeoutException()));
        for (int i = 0; i < 10; i++)
        {
            sendArray1[i] = fixture.Create<ExampleMessage1>();
        }

        var sendArray2 = new ExampleMessage2[10];
        for (int i = 0; i < count; i++)
        {
            sendArray2[i] = fixture.Create<ExampleMessage2>();
        }

        //Act
        foreach (var message1 in sendArray1)
        {
            await link.Server.Send(message1, cancel.Token);
        }

        foreach (var message2 in sendArray2)
        {
            await link.Client.Send(message2, cancel.Token);
        }

        //Assert
        Assert.Equal(count * 2, link.Statistic.TxMessages);
    }

    [Fact]
    public async Task Statistic_SendNullMessage_Fail()
    {
        //Arrange
        var link = Protocol.Create(builder =>
        {
            builder.SetLog(new TestLoggerFactory(_output, TimeProvider.System, "ROUTER"));
            builder.Protocols.RegisterExampleProtocol();
            builder.Formatters.RegisterSimpleFormatter();
        }).CreateVirtualConnection();
        
        //Assert
        // ReSharper disable once AssignNullToNotNullAttribute
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await link.Server.Send(null));

    }

    [Fact]
    public async Task Statistic_ParsedBytesCount_Success()
    {
        //Arrange
        uint count = 10;
        var link = Protocol.Create(builder =>
        {
            builder.SetLog(new TestLoggerFactory(_output, TimeProvider.System, "ROUTER"));
            builder.Protocols.RegisterExampleProtocol();
            builder.Formatters.RegisterSimpleFormatter();
        }).CreateVirtualConnection();
        var fixture = new Fixture();
        var sendArray1 = new ExampleMessage1[count];
        var tcs = new TaskCompletionSource();
        var cancel = new CancellationTokenSource();
        cancel.Token.Register(() => tcs.TrySetException(new TimeoutException()));
        for (var i = 0; i < count; i++)
        {
            sendArray1[i] = fixture.Create<ExampleMessage1>();
        }

        var size = 0;
        //Act
        foreach (var message1 in sendArray1)
        {
            await link.Server.Send(message1, cancel.Token);
            size += message1.GetByteSize();
        }

        foreach (var message1 in sendArray1)
        {
            await link.Client.Send(message1, cancel.Token);
        }

        //Assert
        Assert.Equal((uint)size * 2, link.Statistic.ParsedBytes);
    }
}