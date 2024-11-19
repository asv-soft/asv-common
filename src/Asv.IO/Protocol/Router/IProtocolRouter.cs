using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using R3;
using ZLogger;

namespace Asv.IO;

public class ProtocolRouterConfig
{
    
}

public class PortConfig
{
    public required string Name { get; set; }
    public required string ConnectionString { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public interface IProtocolRouter : IProtocolMessagePipe
{
    void AddPort(IProtocolPort port);
    bool RemovePort(IProtocolPort port);
    IReadOnlyObservableList<IProtocolPort> Ports { get; }
}



public class ProtocolRouter:IProtocolRouter
{
    private readonly ProtocolRouterConfig _config;
    private readonly ImmutableArray<IProtocolProcessingFeature> _features;
    private readonly IProtocolCore _core;
    private readonly Subject<IProtocolMessage> _internalMessageReceived = new();
    private readonly Subject<IProtocolMessage> _onMessageSent = new();
    private readonly ObservableList<IProtocolPort> _ports = new();
    private readonly List<(IProtocolPort,IDisposable)> _portSubscriptions = new();
    private readonly ReaderWriterLockSlim _portLock = new ();
    private readonly ILogger<ProtocolRouter> _logger;
    private readonly IDisposable _sub1;
    private readonly CancellationTokenSource _disposeCancel = new();

    public ProtocolRouter(ProtocolRouterConfig config, IEnumerable<IProtocolProcessingFeature> features, IProtocolCore core)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(core);
        _logger = core.LoggerFactory.CreateLogger<ProtocolRouter>();
        _config = config;
        _features = [..features];
        _core = core;
        _sub1 = _internalMessageReceived.Subscribe(RouteReceivedMessage);
    }
    
    private async void RouteReceivedMessage(IProtocolMessage message)
    {
        try
        {
            foreach (var feature in _features)
            {
                await feature.ProcessReceivedMessage(ref message, this, _disposeCancel.Token);
            }
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, $"Error while routing message: {e.Message}");
        }
    }

    public Observable<IProtocolMessage> OnMessageReceived => _internalMessageReceived;
    public Observable<IProtocolMessage> OnMessageSent => _onMessageSent;
    public async ValueTask Send(IProtocolMessage message, CancellationToken cancel = default)
    {
        _portLock.EnterReadLock();
        try
        {
            foreach (var port in _ports)
            {
                await port.Send(message, cancel);
            }
        }
        finally
        {
            _portLock.EnterReadLock();   
        }
    }

    public void AddFeature(string featureId)
    {
        throw new NotImplementedException();
    }

    public IProtocolPort AddPort(PortConfig config)
    {
        var port = _portFactory.Create(config.ConnectionString);
        port.OnMessageReceived.Subscribe(_internalMessageReceived.AsObserver());
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

    public void AddPort(IProtocolPort port)
    {
        _portLock.EnterWriteLock();
        try
        {
            var sub = port.OnMessageReceived.Subscribe(_internalMessageReceived.AsObserver());
            _portSubscriptions.Add(sub);
            _ports.Add(port);
        }
        finally
        {
            _portLock.ExitWriteLock();   
        }
        
    }

    public bool RemovePort(IProtocolPort port)
    {
        _portLock.EnterWriteLock();
        try
        {
            if (!_ports.Remove(port)) return false;
            port.Dispose();
            return true;
        }
        finally
        {
            _portLock.ExitWriteLock();   
        }

        
    }

    public IReadOnlyObservableList<IProtocolPort> Ports => _ports;
}