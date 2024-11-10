using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using Xunit;
using R3;

namespace Asv.IO.Test;

[Collection("Sequential")]
public class TcpTest
{
    private readonly FakeTimeProvider _timeProvider = new();

    public static TcpClientPort CreateTcpClient(string host = "127.0.0.1", int port = 5050) => new(new TcpPortConfig
    {
        Host = host,
        Port = port,
        IsServer = false
    });

    public static TcpServerPort CreateTcpServer(string host = "127.0.0.1", int port = 5050) => new(new TcpPortConfig
    {
        Host = host,
        Port = port,
        IsServer = true
    });
    
    [Fact(Skip="This test can be performed only on a local machine.")]
    public void TcpClientConnectionErrorTest()
    {
        var client = CreateTcpClient(port: 2005);
        
        client.Enable();
        
        for (var i = 0; i < 10; i++)
        {
            _timeProvider.Advance(TimeSpan.FromSeconds(1));
        }
        
        Assert.Equal(PortState.Error, client.State.CurrentValue);
        
        client.Disable();
    }
    
    [Fact(Skip="This test can be performed only on a local machine.")]
    public void TcpServerConnectionSuccessTest()
    {
        var server = CreateTcpServer(port: 2004);
        
        server.Enable();
        
        for (var i = 0; i < 10; i++)
        {
            _timeProvider.Advance(TimeSpan.FromSeconds(1));
        }
        
        Assert.Equal(PortState.Connected, server.State.CurrentValue);
        
        server.Disable();
    }
    
    [Fact(Skip="This test can be performed only on a local machine.")]
    public void TcpClientServerConnectionTest()
    {
        var client = CreateTcpClient(port: 2003);
        var server = CreateTcpServer(port: 2003);
        
        client.Enable();
        server.Enable();
        
        for (var i = 0; i < 10; i++)
        {
            _timeProvider.Advance(TimeSpan.FromSeconds(1));
        }
        
        Assert.Equal(PortState.Connected, client.State.CurrentValue);
        Assert.Equal(PortState.Connected, server.State.CurrentValue);
        
        client.Disable();
        server.Disable();
    }
    
    [Fact(Skip="This test can be performed only on a local machine.")]
    public async Task TcpClientServerDataTransferTest()
    {
        var client = CreateTcpClient(port: 2002);
        var server = CreateTcpServer(port: 2002);
        
        client.Enable();
        server.Enable();
        
        for (var i = 0; i < 10; i++)
        {
            if (client.State.CurrentValue == PortState.Connected && server.State.CurrentValue == PortState.Connected) break;
            _timeProvider.Advance(TimeSpan.FromSeconds(1));
        }
        
        var originData = new byte[Random.Shared.Next(32, 1024)];
        var receivedData = Array.Empty<byte>();

        Random.Shared.NextBytes(originData);

        using var disp = server.OnReceive.Subscribe(data =>
        {
            receivedData = data;
        });

        Assert.True(await client.Send(originData, originData.Length, default));

        _timeProvider.Advance(TimeSpan.FromSeconds(5));
        
        Assert.Equal(originData, receivedData);
        
        client.Disable();
        server.Disable();
    }
}
