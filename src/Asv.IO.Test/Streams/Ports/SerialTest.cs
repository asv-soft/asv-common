using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using Xunit;
using R3;

namespace Asv.IO.Test;

[Collection("Sequential")]
public class SerialTest
{
    private readonly FakeTimeProvider _timeProvider = new();

    public static CustomSerialPort CreateSerialPort(string name = "COM0") => new(new SerialPortConfig
    {
        PortName = name
    });
    
    [Fact(Skip="This test can be performed only on a local machine.")]
    public void SerialClientConnectionSuccessTest()
    {
        var client = CreateSerialPort("COM6");
        
        client.Enable();
        
        for (var i = 0; i < 10; i++)
        {
            _timeProvider.Advance(TimeSpan.FromSeconds(1)); 
        }
        
        Assert.Equal(PortState.Connected, client.State.CurrentValue);
        
        client.Disable();
    }
    
    [Fact(Skip="This test can be performed only on a local machine.")]
    public void SerialServerConnectionSuccessTest()
    {
        var server = CreateSerialPort("COM5");
        
        server.Enable();
        
        for (var i = 0; i < 10; i++)
        {
            _timeProvider.Advance(TimeSpan.FromSeconds(1));
        }
        
        Assert.Equal(PortState.Connected, server.State.CurrentValue);
        
        server.Disable();
    }
    
    [Fact(Skip="This test can be performed only on a local machine.")]
    public void SerialClientServerConnectionTest()
    {
        var client = CreateSerialPort("COM3");
        var server = CreateSerialPort("COM4");
        
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
    public async Task SerialClientServerDataTransferTest()
    {
        var client = CreateSerialPort("COM1");
        var server = CreateSerialPort("COM2");
        
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
