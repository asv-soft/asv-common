using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace Asv.IO;

public class UdpProtocolPortConfig:ProtocolPortConfig
{
    public string LocalHost { get; set; } = "127.0.0.1";
    public int LocalPort { get; set; } = 8080;
    public string? RemoteHost { get; set; }
    public int? RemotePort { get; set; }
}

public class UdpProtocolPort:ProtocolPort
{
    public const string Scheme = "udp";
    
    public UdpProtocolPort(UdpProtocolPortConfig config, IEnumerable<IProtocolProcessingFeature> filters, Func<IEnumerable<IProtocolParser>> parserFactory, IProtocolCore core) 
        : base($"{Scheme}_{config.LocalHost}_{config.LocalPort}", config, core)
    {
        
    }

    protected override void InternalSafeDisable()
    {
        throw new NotImplementedException();
    }

    protected override void InternalSafeEnable(CancellationToken token)
    {
        throw new NotImplementedException();
    }
}