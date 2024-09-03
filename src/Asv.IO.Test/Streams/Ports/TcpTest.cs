using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Asv.IO.Test;

[Collection("Sequential")]
public class TcpTest
{
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
    public async Task TcpClientConnectionErrorTest()
    {
        var client = CreateTcpClient(port:2005);
        
        client.Enable();
        
        for (int i = 0; i < 10; i++)
        {
            Thread.Sleep(1000);
        }
        
        Assert.Equal(PortState.Error, client.State.Value);
        
        client.Disable();
    }
    
    [Fact(Skip="This test can be performed only on a local machine.")]
    public async Task TcpServerConnectionSuccessTest()
    {
        var server = CreateTcpServer(port:2004);
        
        server.Enable();
        
        for (int i = 0; i < 10; i++)
        {
            Thread.Sleep(1000);
        }
        
        Assert.Equal(PortState.Connected, server.State.Value);
        
        server.Disable();
    }
    
    [Fact(Skip="This test can be performed only on a local machine.")]
    public async Task TcpClientServerConnectionTest()
    {
        var client = CreateTcpClient(port:2003);
        var server = CreateTcpServer(port:2003);
        
        client.Enable();
        server.Enable();
        
        for (int i = 0; i < 10; i++)
        {
            Thread.Sleep(1000);
        }
        
        Assert.Equal(PortState.Connected, client.State.Value);
        Assert.Equal(PortState.Connected, server.State.Value);
        
        client.Disable();
        server.Disable();
    }
    
    [Fact(Skip="This test can be performed only on a local machine.")]
    public async Task TcpClientServerDataTransferTest()
    {
        var client = CreateTcpClient(port:2002);
        var server = CreateTcpServer(port:2002);
        
        client.Enable();
        server.Enable();
        
        for (int i = 0; i < 10; i++)
        {
            if (client.State.Value == PortState.Connected && server.State.Value == PortState.Connected) break;
            Thread.Sleep(1000);
        }
        
        var originData = new byte[Random.Shared.Next(32, 1024)];
        var recievedData = Array.Empty<byte>();

        Random.Shared.NextBytes(originData);

        using var disp = server.Subscribe(data =>
        {
            recievedData = data;
        });

        Assert.True(await client.Send(originData, originData.Length, default));

        Thread.Sleep(5000);
        
        Assert.Equal(originData, recievedData);
        
        client.Disable();
        server.Disable();
    }
    
}