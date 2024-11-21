using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

public interface IProtocolRouter : IProtocolConnection
{
    PortConfig[] GetConfig();
    IProtocolPort AddPort(PortConfig port);
    bool RemovePort(string portId);
    IReadOnlyObservableList<IProtocolPort> Ports { get; }
}

public sealed class ProtocolRouter: ProtocolConnection, IProtocolRouter
{
    private readonly IProtocol _protocol;
    private readonly ObservableList<IProtocolPort> _ports = new();
    private readonly ReaderWriterLockSlim _portLock = new ();
    private readonly ILogger<ProtocolRouter> _logger;
    private readonly List<PortConfig> _portConfig = new();

    public ProtocolRouter(string id,IProtocol protocol)
        :base(id,protocol.Features,protocol.Core)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        _protocol = protocol;
        _logger = protocol.Core.LoggerFactory.CreateLogger<ProtocolRouter>();
    }
    
    public override async ValueTask Send(IProtocolMessage message, CancellationToken cancel = default)
    {
        if (IsDisposed) return;
        var newMessage = await InternalFilterTxMessage(message);
        if (newMessage == null) return;
        _portLock.EnterReadLock();
        try
        {
            foreach (var port in _ports)
            {
                await port.Send(newMessage, cancel);
            }
        }
        finally
        {
            _portLock.EnterReadLock();   
        }
    }

    public PortConfig[] GetConfig()
    {
        _portLock.EnterWriteLock();
        try
        {
            return _portConfig.ToArray();
        }
        finally
        {
            _portLock.ExitWriteLock();
        }
    }

    public IProtocolPort AddPort(PortConfig port)
    {
        var p = _protocol.CreatePort(port.ConnectionString);
        p.SetPortName(port.Name);
        p.OnRxMessage.Subscribe(InternalPublishRxMessage, InternalPublishRxError, _ => { });
        _logger.ZLogInformation($"Port {port.Name}[{port}] added to router");
        _portLock.EnterWriteLock();
        try
        {
            _ports.Add(p);
            _portConfig.Add(port);
        }
        finally
        {
            _portLock.ExitWriteLock();
        }
        if (port.IsEnabled)
        {
            p.Enable();
        }
        else
        {
            p.Disable();
        }
        return p;
    }

    public bool RemovePort(string portId)
    {
        _portLock.EnterWriteLock();
        try
        {
            var port = _ports.FirstOrDefault(x => x.Id == portId);
            if (port == null) return false;
            _logger.ZLogInformation($"Port {port.Id} removed from router");
            var index = _ports.IndexOf(port);
            _ports.Remove(port);
            _portConfig.RemoveAt(index);
            port.Dispose();
            return true;
        }
        finally
        {
            _portLock.ExitWriteLock();
        }
    }

    public IReadOnlyObservableList<IProtocolPort> Ports => _ports;

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _portLock.EnterWriteLock();
            try
            {
                foreach (var port in _ports)
                {
                    port.Dispose();
                }
                _ports.Clear();
                _portConfig.Clear();
            }
            finally
            {
                _portLock.ExitWriteLock();
                _portLock.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        _portLock.EnterWriteLock();
        try
        {
            foreach (var port in _ports)
            {
                await port.DisposeAsync();
            }
            _ports.Clear();
            _portConfig.Clear();
        }
        finally
        {
            _portLock.ExitWriteLock();
            _portLock.Dispose();
        }
        await base.DisposeAsyncCore();
    }

    #endregion
}