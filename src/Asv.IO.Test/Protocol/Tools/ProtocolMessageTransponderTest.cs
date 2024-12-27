#nullable enable
using System;
using System.Threading.Tasks;
using Asv.Cfg.Test;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using R3;
using TimeProviderExtensions;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test;

[TestSubject(typeof(ProtocolMessageTransponder<ExampleMessage1>))]
public class ProtocolMessageTransponderTest
{
    private readonly ExampleMessage1 _originMessage;
    private readonly IVirtualConnection _virtualConnection;
    private readonly ManualTimeProvider _time;
    private readonly ILogger _logger;
    private readonly IProtocolFactory _protocol;

    public ProtocolMessageTransponderTest(ITestOutputHelper logger)
    {
        _originMessage = new ExampleMessage1();
        _time = new ManualTimeProvider();
        var logFactory = new TestLoggerFactory(logger, TimeProvider.System, "ROUTER");
        _logger = logFactory.CreateLogger("Test");
        _protocol = Protocol.Create(builder =>
        {
            builder.SetLog(logFactory);
            builder.SetTimeProvider(_time);
            builder.Protocols.RegisterExampleProtocol();
        });
        _virtualConnection = _protocol.CreateVirtualConnection();
        
       
    }
    
    
    [Theory]
    [InlineData(10000, 1)]
    [InlineData(1000, 5)]
    [InlineData(100, 10)]
    public async Task EverySendCallback_ChangeMessage_Success(int timeSpanMs, int count)
    {
        ExampleMessage1? lastMessage = null;
        await using var transponder = new ProtocolMessageTransponder<ExampleMessage1>(
            _originMessage,
            x =>
            {
                x.Value1++;
            },
            _virtualConnection.Client,
            _protocol.TimeProvider,
            _protocol.LoggerFactory);
        
        _virtualConnection.Server.RxFilterByType<ExampleMessage1>().Subscribe(p =>
        {
            lastMessage = p;
        });
        
        transponder.Start(TimeSpan.FromMilliseconds(timeSpanMs), TimeSpan.FromMilliseconds(timeSpanMs));
        _time.Advance(TimeSpan.FromMilliseconds(timeSpanMs * count));
        
        Assert.NotNull(lastMessage);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        Assert.Equal(count,lastMessage.Value1);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        
    }
    
    [Theory]
    [InlineData(10000, 1)]
    [InlineData(1000, 5)]
    [InlineData(100, 10)]
    public async Task Set_ChangeMessage_Success(int timeSpanMs, int count)
    {
        ExampleMessage1? lastMessage = null;
        await using var transponder = new ProtocolMessageTransponder<ExampleMessage1>(
            _originMessage,
            x =>
            {
                x.Value1++;
            },
            _virtualConnection.Client,
            _protocol.TimeProvider,
            _protocol.LoggerFactory);
        
        _originMessage.Value2 = 1;
        
        _virtualConnection.Server.RxFilterByType<ExampleMessage1>().Subscribe(p =>
        {
            lastMessage = p;
        });
        
        transponder.Start(TimeSpan.FromMilliseconds(timeSpanMs), TimeSpan.FromMilliseconds(timeSpanMs));
        _time.Advance(TimeSpan.FromMilliseconds(timeSpanMs * count));
        
        Assert.NotNull(lastMessage);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        Assert.Equal(count,lastMessage.Value1);
        Assert.Equal(1,lastMessage.Value2);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        
        transponder.Set(x=>x.Value2 = 5 );
        _time.Advance(TimeSpan.FromMilliseconds(timeSpanMs * count));
        Assert.Equal(count * 2, lastMessage.Value1);
        Assert.Equal(5,lastMessage.Value2);
        
        
    }
}