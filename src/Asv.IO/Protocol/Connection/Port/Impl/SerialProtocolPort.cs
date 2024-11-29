using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO;

public class SerialProtocolPortConfig(Uri cs):ProtocolPortConfig(cs)
{
    public static SerialProtocolPortConfig CreateDefault() => new(new Uri($"{SerialProtocolPort.Scheme}:COM1?br=115200"));
    
    public const string DataBitsKey = "data_bits";
    public const int DataBitsDefault = 8;
    public int DataBits
    {
        get => Query[DataBitsKey] != null && int.TryParse(Query[DataBitsKey], out var result) ? result : DataBitsDefault;
        set => Query[DataBitsKey] = value.ToString();
    }
    
    public const string BoundRateKey = "br";
    public const int BoundRateDefault = 115200;
    public int BoundRate
    {
        get => Query[BoundRateKey] != null && int.TryParse(Query[BoundRateKey], out var result) ? result : BoundRateDefault;
        set => Query[BoundRateKey] = value.ToString();
    }

    public const string ParityKey = "parity";
    public const Parity ParityDefault = Parity.None;
    public Parity Parity
    {
        get
        {
            var parity = Query[ParityKey];
            if (parity != null && Enum.TryParse<Parity>(parity, true, out var result))
            {
                return result;
            }
            return ParityDefault;
        }
        set => Query[ParityKey] = value.ToString();
    }

    public const string StopBitsKey = "stop";
    public const StopBits StopBitsDefault = StopBits.One;
    public StopBits StopBits
    {
        get
        {
            var stopBits = Query[StopBitsKey];
            if (stopBits != null && Enum.TryParse<StopBits>(stopBits, true, out var result))
            {
                return result;
            }
            return StopBitsDefault;
        }
        set => Query[StopBitsKey] = value.ToString();
    }

    public string? PortName
    {
        get => Path;
        set => Path = value;
    }

    public int WriteTimeout
    {
        get
        {
            var writeTimeout = Query["wt"];
            if (writeTimeout != null && int.TryParse(writeTimeout, out var result))
            {
                return result;
            }
            return 200;
        }
        set => Query["wt"] = value.ToString();
    }

    public int WriteBufferSize
    {
        get
        {
            var writeBufferSize = Query["wb"];
            if (writeBufferSize != null && int.TryParse(writeBufferSize, out var result))
            {
                return result;
            }
            return 40960;
        }
        set => Query["wb"] = value.ToString();
    }

    
}

public sealed class SerialProtocolPort : ProtocolPort<SerialProtocolPortConfig>
{
    public const string Scheme = "serial";
    public static readonly PortTypeInfo Info = new(Scheme, "Serial port");
    private readonly SerialProtocolPortConfig _config;
    private readonly IProtocolContext _context;
    private SerialPort? _serial;
    private SerialProtocolEndpoint? _pipe;

    public SerialProtocolPort(SerialProtocolPortConfig config,
        IProtocolContext context,
        IStatisticHandler statistic) : base($"{Scheme}_{config.PortName}", config, context, true, statistic)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(context);
        _config = config;
        _context = context;
    }


    public override PortTypeInfo TypeInfo => Info;

    protected override void InternalSafeDisable()
    {
        if (_serial != null)
        {
            var serial = _serial;
            if (serial.IsOpen) serial.Close();
            serial.Dispose();
            serial = null;
        }
        if (_pipe != null)
        {
            _pipe.Dispose();
            _pipe = null;
        }
        
    }

    protected override void InternalSafeEnable(CancellationToken token)
    {
        _serial = new SerialPort(_config.PortName, _config.BoundRate, _config.Parity, _config.DataBits, _config.StopBits)
        {
            WriteBufferSize = _config.WriteBufferSize,
            WriteTimeout = _config.WriteTimeout,
            ReadBufferSize = _config.ReadBufferSize,
            ReadTimeout = _config.ReadTimeout,
        };
        _serial.Open();
        _pipe = new SerialProtocolEndpoint(
            _serial,
            ProtocolHelper.NormalizeId(
                $"{Id}_{_config.BoundRate}_{_config.DataBits}_{_config.Parity}_{_config.StopBits}"),
            _config, InternalCreateParsers(), _context, StatisticHandler);
        InternalAddConnection(_pipe);
        
    }

    

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_serial != null)
            {
                _serial.Close();
                _serial.Dispose();
                _serial = null;
            }
            if (_pipe != null)
            {
                _pipe.Dispose();
                _pipe = null;
            }
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_serial != null)
        {
            _serial.Close();
            _serial.Dispose();
            _serial = null;
        }
        if (_pipe != null)
        {
            await _pipe.DisposeAsync();
            _pipe = null;
        }
        await base.DisposeAsyncCore();
    }

    #endregion
}

public static class SerialProtocolPortHelper
{

    public static IProtocolPort AddSerialPort(this IProtocolRouter src, Action<SerialProtocolPortConfig> edit)
    {
        var cfg = SerialProtocolPortConfig.CreateDefault();
        edit(cfg);
        return src.AddPort(cfg.AsUri());
    }
    public static void RegisterSerialPort(this IProtocolBuilder builder)
    {
        builder.RegisterPort(SerialProtocolPort.Info, 
            (cs,  context,stat) 
                => new SerialProtocolPort(new SerialProtocolPortConfig(cs),context,stat));
    }
}