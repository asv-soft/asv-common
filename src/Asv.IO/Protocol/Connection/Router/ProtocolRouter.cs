using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNext.Threading;
using Microsoft.Extensions.Logging;
using ObservableCollections;
using R3;
using ZLogger;

namespace Asv.IO;

public sealed class ProtocolRouter: ProtocolConnection, IProtocolRouter
{
    private readonly IProtocol _protocol;
    private readonly ObservableList<IProtocolPort> _ports = new();
    private readonly AsyncReaderWriterLock _portLock = new ();
    private readonly ILogger<ProtocolRouter> _logger;
    private readonly List<PortConfig> _portConfig = new();

    public ProtocolRouter(string id, IProtocol protocol, IStatisticHandler? statistic = null)
        :base(ProtocolHelper.NormalizeId(id),protocol.Features, TODO, TODO,protocol.Core,statistic)
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
        await _portLock.EnterReadLockAsync(cancel);
        try
        {
            foreach (var port in _ports)
            {
                await port.Send(newMessage, cancel);
            }
        }
        finally
        {
            _portLock.Release();   
        }
    }

    public async Task<PortConfig[]> GetConfig()
    {
        await _portLock.EnterWriteLockAsync();
        try
        {
            return _portConfig.ToArray();
        }
        finally
        {
            _portLock.Release();
        }
    }

    public async Task<IProtocolPort> AddPort(PortConfig port)
    {
        var p = _protocol.AddPort(port.ConnectionString);
        p.SetPortName(port.Name);
        p.OnRxMessage.Subscribe(InternalPublishRxMessage, InternalPublishRxError, _ => { });
        _logger.ZLogInformation($"Port {port.Name}[{port}] added to router");
        await _portLock.EnterWriteLockAsync();
        try
        {
            _ports.Add(p);
            _portConfig.Add(port);
        }
        finally
        {
            _portLock.Release();
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

    public async Task<bool> RemovePort(string portId, CancellationToken cancel)
    {
        await _portLock.EnterWriteLockAsync(cancel);
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
            _portLock.Release();
        }
    }

    public IReadOnlyObservableList<IProtocolPort> Ports => _ports;

    #region Dispose

    protected override async void Dispose(bool disposing)
    {
        if (disposing)
        {
            await _portLock.EnterWriteLockAsync();
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
                _portLock.Release();
                _portLock.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await _portLock.EnterWriteLockAsync();
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
            _portLock.Release();
            _portLock.Dispose();
        }
        await base.DisposeAsyncCore();
    }

    #endregion
}