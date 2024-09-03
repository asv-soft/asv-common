using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Asv.IO.Test;

[Collection("Sequential")]
public class AesTest
{
    public static AesCryptoPort CreateAesCryptoPort(PortBase port) => new(port, new AesCryptoPortConfig
    {
        Key = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6 },
        InitVector = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6 }
    });

    #region TCP
    
    [Fact(Skip="This test can be performed only on a local machine.")]
    public async Task AesTcpClientAndServerDataTest()
    {
        var client = TcpTest.CreateTcpClient(port:1000);
        var server = TcpTest.CreateTcpServer(port:1000);
        var aesClient = CreateAesCryptoPort(client);
        var aesServer = CreateAesCryptoPort(server);
        aesServer.Enable();
        aesClient.Enable();

        for (int i = 0; i < 10; i++)
        {
            if (aesClient.State.Value == PortState.Connected && aesServer.State.Value == PortState.Connected) break;
            Thread.Sleep(1000);
        }

        var originData = new byte[Random.Shared.Next(32, 1024)];
        var recievedData = Array.Empty<byte>();

        Random.Shared.NextBytes(originData);

        using var disp = aesServer.Subscribe(data =>
        {
            recievedData = data;
        });

        Assert.True(await aesClient.Send(originData, originData.Length, default));

        Thread.Sleep(3000);
        
        Assert.Equal(originData, recievedData);          
        
        aesServer.Disable();
        aesClient.Disable();
    }

    [Fact(Skip="This test can be performed only on a local machine.")]
    public async Task AesTcpClientDataTest()
    {
        var client = TcpTest.CreateTcpClient(port:1001);
        var server = TcpTest.CreateTcpServer(port:1001);
        var aesClient = CreateAesCryptoPort(client);
        server.Enable();
        aesClient.Enable();

        for (int i = 0; i < 10; i++)
        {
            if (aesClient.State.Value == PortState.Connected && server.State.Value == PortState.Connected) break;
            Thread.Sleep(1000);
        }

        var originData = new byte[Random.Shared.Next(32, 1024)];
        var recievedData = Array.Empty<byte>();

        Random.Shared.NextBytes(originData);

        using var disp = server.Subscribe(data =>
        {
            recievedData = data;
        });

        Assert.True(await aesClient.Send(originData, originData.Length, default));

        Thread.Sleep(3000);
        
        Assert.NotEqual(originData, recievedData);
        
        server.Disable();
        aesClient.Disable();
    }

    [Fact(Skip="This test can be performed only on a local machine.")]
    public async Task AesTcpServerDataTest()
    {
        var client = TcpTest.CreateTcpClient(port: 1002);
        var server = TcpTest.CreateTcpServer(port: 1002);
        var aesServer = CreateAesCryptoPort(server);
        aesServer.Enable();
        client.Enable();

        for (var i = 0; i < 10; i++)
        {
            if (client.State.Value == PortState.Connected && aesServer.State.Value == PortState.Connected) break;
            await Task.Delay(1000);
        }

        var originData = new byte[Random.Shared.Next(32, 1024)];
        var receivedData = Array.Empty<byte>();
        var tcs = new TaskCompletionSource<bool>();

        Random.Shared.NextBytes(originData);

        using var disp = aesServer.Subscribe(data =>
        {
            receivedData = data;
            tcs.SetResult(true);
        });


        await client.Send(originData, originData.Length, default);
        
        await Task.WhenAny(tcs.Task, Task.Delay(3000));

        Assert.Equal(originData, receivedData);
    
        aesServer.Disable();
        client.Disable();
    }

    #endregion

    #region UDP

    [Fact(Skip="This test can be performed only on a local machine.")]
    public async Task AesUdpClientAndServerDataTest()
    {
        var client = UdpTest.CreateUdpPort(remotePort: 1003);
        var server = UdpTest.CreateUdpPort(localPort: 1003);
        var aesClient = CreateAesCryptoPort(client);
        var aesServer = CreateAesCryptoPort(server);
        aesServer.Enable();
        aesClient.Enable();
        
        for (int i = 0; i < 10; i++)
        {
            if (aesClient.State.Value == PortState.Connected && aesServer.State.Value == PortState.Connected) break;
            Thread.Sleep(1000);
        }
        
        var originData = new byte[Random.Shared.Next(32, 1024)];
        var recievedData = Array.Empty<byte>();
        
        Random.Shared.NextBytes(originData);
        
        using var disp = aesServer.Subscribe(data =>
        {
            recievedData = data;
        });
        
        Assert.True(await aesClient.Send(originData, originData.Length, default));
        
        Thread.Sleep(5000);
        
        Assert.Equal(originData, recievedData);
        
        aesServer.Disable();
        aesClient.Disable();
    }
    
    [Fact(Skip="This test can be performed only on a local machine.")]
    public async Task AesUdpClientDataTest()
    {
        var client = UdpTest.CreateUdpPort(remotePort: 1004);
        var server = UdpTest.CreateUdpPort(localPort: 1004);
        var aesClient = CreateAesCryptoPort(client);
        server.Enable();
        aesClient.Enable();

        for (int i = 0; i < 10; i++)
        {
            if (aesClient.State.Value == PortState.Connected && server.State.Value == PortState.Connected) break;
            Thread.Sleep(1000);
        }

        var originData = new byte[Random.Shared.Next(32, 1024)];
        var recievedData = Array.Empty<byte>();

        Random.Shared.NextBytes(originData);

        using var disp = server.Subscribe(data =>
        {
            recievedData = data;
        });

        Assert.True(await aesClient.Send(originData, originData.Length, default));

        Thread.Sleep(5000);
        
        Assert.NotEqual(originData, recievedData);
        
        server.Disable();
        aesClient.Disable();
    }

    [Fact(Skip="This test can be performed only on a local machine.")]
    public async Task AesUdpServerDataTest()
    {
        var client = UdpTest.CreateUdpPort(remotePort: 2007);
        var server = UdpTest.CreateUdpPort(localPort: 2007);
        var aesServer = CreateAesCryptoPort(server);
        aesServer.Enable();
        client.Enable();

        for (var i = 0; i < 10; i++)
        {
            if (client.State.Value == PortState.Connected && aesServer.State.Value == PortState.Connected) break;
            Thread.Sleep(1000);
        }

        var originData = new byte[Random.Shared.Next(32, 1024)];
        var recievedData = Array.Empty<byte>();

        Random.Shared.NextBytes(originData);

        using var disp = aesServer.Subscribe(data =>
        {
            recievedData = data;
        });
        
        Assert.True(await client.Send(originData, originData.Length, default));

        Thread.Sleep(5000);
        
        Assert.Equal(originData, recievedData);
        
        aesServer.Disable();
        client.Disable();
    }
    
    #endregion

    #region Serial
    //Can be tested with Virtual Serial Port Driver, if there is no real device.
    //You should create 3 pairs of COM ports [1,2], [3,4], [5,6].
    
    [Fact(Skip="This test can be performed only on a local machine.")]
    public async Task AesSerialClientAndServerDataTest()
    {
        var client = SerialTest.CreateSerialPort("COM1");
        var server = SerialTest.CreateSerialPort("COM2");
        var aesClient = CreateAesCryptoPort(client);
        var aesServer = CreateAesCryptoPort(server);
        aesServer.Enable();
        aesClient.Enable();
        
        for (int i = 0; i < 10; i++)
        {
            if (aesClient.State.Value == PortState.Connected && aesServer.State.Value == PortState.Connected) break;
            Thread.Sleep(1000);
        }
        
        var originData = new byte[Random.Shared.Next(32, 1024)];
        var recievedData = Array.Empty<byte>();
        
        Random.Shared.NextBytes(originData);
        
        using var disp = aesServer.Subscribe(data =>
        {
            recievedData = data;
        });
        
        Assert.True(await aesClient.Send(originData, originData.Length, default));
        
        Thread.Sleep(5000);
        
        Assert.Equal(originData, recievedData);
        
        aesServer.Disable();
        aesClient.Disable();
    }
    
    [Fact(Skip="This test can be performed only on a local machine.")]
    public async Task AesSerialClientDataTest()
    {
        var serials = SerialPort.GetPortNames();
        
        var client = SerialTest.CreateSerialPort("COM3");
        var server = SerialTest.CreateSerialPort("COM4");
        var aesClient = CreateAesCryptoPort(client);
        server.Enable();
        aesClient.Enable();

        for (int i = 0; i < 10; i++)
        {
            if (aesClient.State.Value == PortState.Connected && server.State.Value == PortState.Connected) break;
            Thread.Sleep(1000);
        }

        var originData = new byte[Random.Shared.Next(32, 1024)];
        var recievedData = Array.Empty<byte>();

        Random.Shared.NextBytes(originData);

        using var disp = server.Subscribe(data =>
        {
            recievedData = data;
        });

        Assert.True(await aesClient.Send(originData, originData.Length, default));

        Thread.Sleep(5000);
        
        Assert.NotEqual(originData, recievedData);
        
        server.Disable();
        aesClient.Disable();
    }

    [Fact(Skip="This test can be performed only on a local machine.")]
    public async Task AesSerialServerDataTest()
    {
        var client = SerialTest.CreateSerialPort("COM5");
        var server = SerialTest.CreateSerialPort("COM6");
        var aesServer = CreateAesCryptoPort(server);
        aesServer.Enable();
        client.Enable();

        for (int i = 0; i < 10; i++)
        {
            if (client.State.Value == PortState.Connected && aesServer.State.Value == PortState.Connected) break;
            Thread.Sleep(1000);
        }

        var originData = new byte[Random.Shared.Next(32, 1024)];
        var recievedData = Array.Empty<byte>();

        Random.Shared.NextBytes(originData);

        using var disp = aesServer.Subscribe(data =>
        {
            recievedData = data;
        });

        Assert.True(await client.Send(originData, originData.Length, default));
        
        Thread.Sleep(3000);
        
        Assert.Equal(originData, recievedData);
        
        aesServer.Disable();
        client.Disable();
    }
    
    #endregion
}