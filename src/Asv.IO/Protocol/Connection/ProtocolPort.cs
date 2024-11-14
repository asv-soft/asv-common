using System;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using R3;
using ZLogger;

namespace Asv.IO;

public class ProtocolPortConfig : ProtocolConnectionConfig
{
    
}

public abstract class ProtocolPort : IProtocolPort
{
    private readonly ProtocolPortConfig _config;
    private readonly IPipeCore _core;
    private readonly ILogger<ProtocolPort> _logger;
    private IProtocolConnection[] _connections;
    private readonly ITimer _timer;

    #region Static
    // ReSharper disable once UseCollectionExpression
    // Important !!! (Avoid zero-length array allocations. Use collection expressions) The identity of these arrays matters, so we can't use the shared Array.Empty<T>() instance either explicitly, or indirectly via a collection expression
#pragma warning disable CA1825
    private static readonly IPipeEndpoint[] Disposed = new IPipeEndpoint[0]; 
#pragma warning restore CA1825
    
    public static NameValueCollection ParseQueryString(string requestQueryString)
    {
        var rc = new NameValueCollection();
        var ar1 = requestQueryString.Split('&', '?');
        foreach (var row in ar1)
        {
            if (string.IsNullOrEmpty(row)) continue;
            var index = row.IndexOf('=');
            if (index < 0) continue;
            rc[Uri.UnescapeDataString(row[..index])] = Uri.UnescapeDataString(row[(index + 1)..]); // use Unescape only parts          
        }
        return rc;
    }
    
    public static IProtocolPort Create(string connectionString, IPipeCore core)
    {
        var uri = new Uri(connectionString);
        IProtocolPort? result = null;
        if (TcpPipePortConfig.TryParseFromUri(uri, out var tcp))
        {
            Debug.Assert(tcp != null, nameof(tcp) + " != null");
            if (tcp.IsServer)
            {
                result = new TcpServerPipePort(tcp, core);
            }
            else
            {
                result = new TcpClientPipePort(tcp, core);
            }
        }
        else if (UdpPortConfig.TryParseFromUri(uri, out var udp))
        {
            //result = new UdpPort(udp);
        }
        else if (SerialPortConfig.TryParseFromUri(uri, out var ser))
        {
            //result = new CustomSerialPort(ser, timeProvider, logger);
        }
        else
        {
            throw new Exception($"Connection string '{connectionString}' is invalid");
        }
        throw new Exception($"Connection string '{connectionString}' is invalid");
    }

    #endregion

    protected ProtocolPort(ProtocolPortConfig config, IPipeCore core)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(core);
        _config = config;
        _core = core;
        _logger = core.LoggerFactory.CreateLogger<ProtocolPort>();
        _connections = [];
        _logger.ZLogInformation($"Create port {this} {config}");
        _timer = core.TimeProvider.CreateTimer(RemoveDisposedEndpoints, null, TimeSpan.FromMilliseconds(_config.CheckOldClientsPeriodMs), TimeSpan.FromSeconds(_config.CheckOldClientsPeriodMs));
    }
    
    private void RemoveDisposedEndpoints(object? state)
    {
        var itemsToDelete = Connections.Where(x => x.IsDisposed).ToImmutableArray();
        foreach (var item in itemsToDelete)
        {
            InternalRemovePipe(item);
        }
    }
    protected void InternalRemovePipe(IProtocolConnection pipe)
    {
        if (IsDisposed) return;
        _logger.ZLogInformation($"{this} remove pipe endpoint {pipe}");
        try
        {
            for (;;)
            {
                var pipes = Volatile.Read(ref _connections);
                if (pipes == Disposed) break;
                var count = pipes.Length;
                if (count == 0) break;
                var newPipe = new IProtocolConnection[count - 1];
                for (var i = 0; i < count; i++)
                {
                    if (pipes[i] == pipe)
                    {
                        Array.Copy(pipes, i + 1, newPipe, i + 1, count - i - 1);
                        Array.Copy(pipes, 0, newPipe, i + 1, count - i - 1);
                        break;
                    }
                }
                if (Interlocked.CompareExchange(ref _connections, newPipe, pipes) == pipes)
                {
                    break;
                }
                else
                {
                    Debug.Assert( false,"Remove pipe endpoint failed");
                }
            }
            pipe.Dispose();
        }
        catch (Exception e)
        {
            _logger.ZLogError($"Error on dispose pipe {pipe}: {e.Message}");
        }
        
    }
    
    public string Id { get; }
    public ReadOnlyReactiveProperty<ProtocolException?> Error { get; }
    public ReadOnlyReactiveProperty<ProtocolPortStatus> Status { get; }
    public ReadOnlyReactiveProperty<bool> IsEnabled { get; }
    public TagList Tags { get; }
    public IProtocolConnection[] Connections { get; }
    public Observable<IProtocolConnection[]> OnConnectionsChanged { get; }
    public void Enable()
    {
        throw new System.NotImplementedException();
    }

    public void Disable()
    {
        throw new System.NotImplementedException();
    }

    #region Dispose

    public bool IsDisposed { get; }
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    #endregion

}