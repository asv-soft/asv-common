using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Asv.IO.Test;

[Collection("Sequential")]
public class UdpTest
{
    public static UdpPort CreateUdpPort(
        string localHost = "127.0.0.1",
        int localPort = 2050,
        string remoteHost = "127.0.0.1",
        int remotePort = 2050
    ) =>
        new(
            new UdpPortConfig
            {
                LocalHost = localHost,
                LocalPort = localPort,
                RemoteHost = remoteHost,
                RemotePort = remotePort,
            }
        );

    [Fact(Skip = "This test can be performed only on a local machine.")]
    public async Task UdpClientConnectionSuccessTest()
    {
        var client = CreateUdpPort(localPort: 2005);

        client.Enable();

        for (int i = 0; i < 10; i++)
        {
            Thread.Sleep(1000);
        }

        Assert.Equal(PortState.Connected, client.State.Value);

        client.Disable();
    }

    [Fact(Skip = "This test can be performed only on a local machine.")]
    public async Task UdpServerConnectionSuccessTest()
    {
        var server = CreateUdpPort(localPort: 2004);

        server.Enable();

        for (int i = 0; i < 10; i++)
        {
            Thread.Sleep(1000);
        }

        Assert.Equal(PortState.Connected, server.State.Value);

        server.Disable();
    }

    [Fact(Skip = "This test can be performed only on a local machine.")]
    public async Task UdpClientServerConnectionTest()
    {
        var client = CreateUdpPort(localPort: 2002, remotePort: 2007);
        var server = CreateUdpPort(localPort: 2007, remotePort: 2002);

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
    public async Task UdpClientServerDataTransferTest()
    {
        var client = CreateUdpPort(localPort: 2001, remotePort: 2009);
        var server = CreateUdpPort(localPort: 2009, remotePort: 2001);

        server.Enable();
        client.Enable();

        for (int i = 0; i < 30; i++)
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

        server.Subscribe(data => recievedData = data);

        Assert.True(await client.Send(originData, originData.Length, default));

        Thread.Sleep(3000);

        Assert.Equal(originData, recievedData);

        client.Disable();
        server.Disable();
    }
}
