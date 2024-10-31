using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Asv.IO.Test;

[Collection("Sequential")]
public class SerialTest
{
    // Can be tested with Virtual Serial Port Driver, if there is no real device.
    // You should create 3 pairs of COM ports [1,2], [3,4], [5,6].
    public static CustomSerialPort CreateSerialPort(string name = "COM0") =>
        new(new SerialPortConfig { PortName = name });

    [Fact(Skip = "This test can be performed only on a local machine.")]
    public async Task SerialClientConnectionSuccessTest()
    {
        var client = CreateSerialPort("COM6");

        client.Enable();

        for (int i = 0; i < 10; i++)
        {
            Thread.Sleep(1000);
        }

        Assert.Equal(PortState.Connected, client.State.Value);

        client.Disable();
    }

    [Fact(Skip = "This test can be performed only on a local machine.")]
    public async Task SerialServerConnectionSuccessTest()
    {
        var server = CreateSerialPort("COM5");

        server.Enable();

        for (int i = 0; i < 10; i++)
        {
            Thread.Sleep(1000);
        }

        Assert.Equal(PortState.Connected, server.State.Value);

        server.Disable();
    }

    [Fact(Skip = "This test can be performed only on a local machine.")]
    public async Task SerialClientServerConnectionTest()
    {
        var client = CreateSerialPort("COM3");
        var server = CreateSerialPort("COM4");

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

    [Fact(Skip = "This test can be performed only on a local machine.")]
    public async Task SerialClientServerDataTransferTest()
    {
        var client = CreateSerialPort("COM1");
        var server = CreateSerialPort("COM2");

        client.Enable();
        server.Enable();

        for (int i = 0; i < 10; i++)
        {
            if (
                client.State.Value == PortState.Connected
                && server.State.Value == PortState.Connected
            )
            {
                break;
            }

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
