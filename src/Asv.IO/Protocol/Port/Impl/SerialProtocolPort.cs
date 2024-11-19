using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Ports;
using System.Threading;

namespace Asv.IO;

public class SerialProtocolPortConfig:ProtocolPortConfig
{
    public static SerialProtocolPortConfig Parse(PortArgs args)
    {
        var config = new SerialProtocolPortConfig
        {
            PortName = args.Path ?? throw new ArgumentNullException(nameof(args.Path)),
            BoundRate = int.Parse(args.Query["br"] ?? "115200")
        };

        return config;
    }
    
    public int DataBits { get; set; } = 8;
    public int BoundRate { get; set; } = 115200;
    public Parity Parity { get; set; } = Parity.None;
    public StopBits StopBits { get; set; } = StopBits.One;
    public string PortName { get; set; }
    public int WriteTimeout { get; set; } = 200;
    public int WriteBufferSize { get; set; } = 40960;

}

public class SerialProtocolPort:ProtocolPort
{
    #region Facory

    public static IProtocolPort CreatePort(PortArgs args, IProtocolCore core, ImmutableArray<IProtocolProcessingFeature> features, Func<ImmutableArray<IProtocolParser>> parserFactory)
    {
        var config = SerialProtocolPortConfig.Parse(args);
        return new SerialProtocolPort(config, core, features, parserFactory);
    }

    #endregion
    
    private readonly SerialProtocolPortConfig _config;
    private readonly IProtocolCore _core;
    private readonly IEnumerable<IProtocolProcessingFeature> _features;
    private readonly Func<IEnumerable<IProtocolParser>> _parserFactory;
    private SerialPort? _serial;
    public const string Scheme = "serial";
    
    public SerialProtocolPort(SerialProtocolPortConfig config, IProtocolCore core, IEnumerable<IProtocolProcessingFeature> features, Func<ImmutableArray<IProtocolParser>> parserFactory) 
        : base($"{Scheme}_{config.PortName}", config, core)
    {
        _config = config;
        _core = core;
        _features = features;
        _parserFactory = parserFactory;
        
    }

    protected override void InternalSafeDisable()
    {
        if (_serial != null)
        {
            var serial = _serial;
            serial.Close();
            serial.Dispose();
            serial = null;
        }
    }

    protected override void InternalSafeEnable(CancellationToken token)
    {
        _serial?.Close();
        _serial?.Dispose();
        _serial = new SerialPort(_config.PortName, _config.BoundRate, _config.Parity, _config.DataBits, _config.StopBits)
        {
            WriteBufferSize = _config.WriteBufferSize,
            WriteTimeout = _config.WriteTimeout,
        };
        _serial.Open();
        InternalAddConnection(new SerialProtocolConnection(_serial,$"{Id}_{_config.BoundRate}_{_config.DataBits}_{_config.Parity}_{_config.StopBits}",_config, _parserFactory(), _features, _core));
        
    }
}