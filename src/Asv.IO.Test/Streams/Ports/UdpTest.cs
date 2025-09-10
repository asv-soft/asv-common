using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using R3;
using Xunit;

namespace Asv.IO.Test;

[Collection("Sequential")]
public class UdpTest
{
    private readonly FakeTimeProvider _timeProvider = new();

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
    public void UdpClientConnectionSuccessTest()
    {
        var client = CreateUdpPort(localPort: 2005);

        client.Enable();

        for (var i = 0; i < 10; i++)
        {
            _timeProvider.Advance(TimeSpan.FromSeconds(1));
        }

        Assert.Equal(PortState.Connected, client.State.CurrentValue);

        client.Disable();
    }

    [Fact(Skip = "This test can be performed only on a local machine.")]
    public void UdpServerConnectionSuccessTest()
    {
        var server = CreateUdpPort(localPort: 2004);

        server.Enable();

        for (var i = 0; i < 10; i++)
        {
            _timeProvider.Advance(TimeSpan.FromSeconds(1));
        }

        Assert.Equal(PortState.Connected, server.State.CurrentValue);

        server.Disable();
    }

    [Fact(Skip = "This test can be performed only on a local machine.")]
    public void UdpClientServerConnectionTest()
    {
        var client = CreateUdpPort(localPort: 2002, remotePort: 2007);
        var server = CreateUdpPort(localPort: 2007, remotePort: 2002);

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

    [Fact(Skip = "This test can be performed only on a local machine.")]
    public async Task UdpClientServerDataTransferTest()
    {
        var client = CreateUdpPort(localPort: 2001, remotePort: 2009);
        var server = CreateUdpPort(localPort: 2009, remotePort: 2001);

        server.Enable();
        client.Enable();

        for (var i = 0; i < 30; i++)
        {
            if (
                client.State.CurrentValue == PortState.Connected
                && server.State.CurrentValue == PortState.Connected
            )
            {
                break;
            }

            _timeProvider.Advance(TimeSpan.FromSeconds(1));
        }

        var originData = new byte[Random.Shared.Next(32, 1024)];
        var receivedData = Array.Empty<byte>();

        Random.Shared.NextBytes(originData);

        server.OnReceive.Subscribe(data => receivedData = data);

        Assert.True(await client.Send(originData, originData.Length, default));

        _timeProvider.Advance(TimeSpan.FromSeconds(3));

        Assert.Equal(originData, receivedData);

        client.Disable();
        server.Disable();
    }
}
