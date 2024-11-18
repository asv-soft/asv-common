using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using R3;
using ZLogger;

namespace Asv.IO;

public class PortConfig
{
    public required string Name { get; set; }
    public required string ConnectionString { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public interface IProtocolRouter : IProtocolMessagePipe
{
    IProtocolPort AddPort(PortConfig config);
    bool RemovePort(IProtocolPort port);
    IReadOnlyObservableList<IProtocolPort> Ports { get; }
}



public class ProtocolRouter:IProtocolRouter
{
    private readonly IProtocolPortFactory _factory;
    private readonly IProtocolCore _core;
    private readonly Subject<IProtocolMessage> _onMessageReceived = new();
    private readonly Subject<IProtocolMessage> _onMessageSent = new();
    private readonly ObservableList<IProtocolPort> _ports = new();
    private readonly ReaderWriterLockSlim _portLock = new ();
    private readonly ILogger<ProtocolRouter> _logger;
    private readonly IDisposable _sub1;

    public ProtocolRouter(IProtocolPortFactory factory, IProtocolCore core)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(core);
        _logger = core.LoggerFactory.CreateLogger<ProtocolRouter>();
        _factory = factory;
        _core = core;
        _sub1 = _onMessageReceived.Subscribe(RouteReceivedMessage);
    }

    private async void RouteReceivedMessage(IProtocolMessage msg)
    {
        try
        {
            await Send(msg);
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, $"Error while routing message: {e.Message}");
        }
    }


    public Observable<IProtocolMessage> OnMessageReceived => _onMessageReceived;
    public Observable<IProtocolMessage> OnMessageSent => _onMessageSent;
    public async ValueTask Send(IProtocolMessage message, CancellationToken cancel = default)
    {
        _portLock.EnterReadLock();
        try
        {
            foreach (var connection in _ports)
            {
                await connection.Send(message, cancel);
            }
        }
        finally
        {
            _portLock.EnterReadLock();   
        }
    }

    public IProtocolPort AddPort(PortConfig config)
    {
        var port = _factory.Create(config.ConnectionString);
        port.OnMessageReceived.Subscribe(_onMessageReceived.AsObserver());
        port.Tags.SetPortName(config.Name);
        _ports.Add(port);
        if (config.IsEnabled)
        {
            port.Enable();
        }
        else
        {
            port.Disable();
        }
        return port;
    }

    public bool RemovePort(IProtocolPort port)
    {
        if (!_ports.Remove(port)) return false;
        port.Dispose();
        return true;
    }

    public IReadOnlyObservableList<IProtocolPort> Ports => _ports;
}