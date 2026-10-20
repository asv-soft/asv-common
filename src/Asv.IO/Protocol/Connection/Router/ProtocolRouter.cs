using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using R3;
using ZLogger;

namespace Asv.IO;

public sealed class ProtocolRouter : ProtocolConnection, IProtocolRouter
{
    private readonly ILogger<ProtocolRouter> _logger;
    private readonly Lock _sync = new();
    private ImmutableArray<IProtocolPort> _ports = [];
    private readonly Subject<IProtocolPort> _portAdded = new();
    private readonly Subject<IProtocolPort> _portRemoved = new();
    private readonly Subject<IProtocolPort> _portUpdated = new();

    internal ProtocolRouter(
        string id,
        IProtocolContext context,
        IStatisticHandler? statistic = null
    )
        : base(id, context, statistic)
    {
        _logger = context.LoggerFactory.CreateLogger<ProtocolRouter>();
    }

    public override async ValueTask Send(
        IProtocolMessage message,
        CancellationToken cancel = default
    )
    {
        ArgumentNullException.ThrowIfNull(message);
        if (IsDisposed)
        {
            return;
        }

        cancel.ThrowIfCancellationRequested();
        var newMessage = await InternalFilterTxMessage(message);
        if (newMessage == null)
        {
            return;
        }

        var ports = _ports;
        foreach (var port in ports)
        {
            await port.Send(newMessage, cancel).ConfigureAwait(false);
        }
        InternalPublishTxMessage(newMessage);
    }

    public Observable<IProtocolPort> PortUpdated => _portUpdated;

    public IProtocolPort AddPort(Uri connectionString)
    {
        ThrowIfDisposed();
        if (!Context.PortFactory.TryGetValue(connectionString.Scheme, out var factory))
        {
            throw new InvalidOperationException($"Port scheme {connectionString.Scheme} not found");
        }
        var port = factory(connectionString, Context, StatisticHandler);
        try
        {
            lock (_sync)
            {
                ThrowIfDisposed();
                _ports = _ports.Add(port);

                // The owned port releases these subscriptions when it is disposed.
                if (port is ProtocolConnection connection)
                {
                    connection.SubscribeRx(InternalPublishRxMessageAsync);
                }
                else
                {
                    port.OnRxMessage.Subscribe(InternalPublishRxMessage);
                }
                port.IsEnabled.Subscribe(port, (_, value) => PublishPortUpdated(value));
                ThrowIfDisposed();
                if (port.Config.IsEnabled && _ports.Contains(port))
                {
                    port.Enable();
                }
                ThrowIfDisposed();
                if (_ports.Contains(port))
                {
                    _portAdded.OnNext(port);
                }
            }
            _logger.ZLogInformation($"{this} add {port}");
            return port;
        }
        catch
        {
            lock (_sync)
            {
                _ports = _ports.Remove(port);
            }
            port.Dispose();
            throw;
        }
    }

    private void PublishPortUpdated(IProtocolPort port)
    {
        lock (_sync)
        {
            if (!IsDisposed && _ports.Contains(port))
            {
                _portUpdated.OnNext(port);
            }
        }
    }

    public void RemovePort(IProtocolPort port)
    {
        lock (_sync)
        {
            if (!_ports.Contains(port))
            {
                return;
            }
            _ports = _ports.Remove(port);
            if (!IsDisposed)
            {
                _portRemoved.OnNext(port);
            }
        }
        port.Dispose();
        _logger.ZLogInformation($"{this} remove {port}");
    }

    public ImmutableArray<ProtocolInfo> AvailableProtocols => Context.AvailableProtocols;
    public ImmutableArray<PortTypeInfo> AvailablePortTypes => Context.AvailablePortTypes;
    public ImmutableArray<IProtocolPort> Ports => _ports;

    public Observable<IProtocolPort> PortAdded => _portAdded;
    public Observable<IProtocolPort> PortRemoved => _portRemoved;

    public override string ToString()
    {
        return $"[ROUTER]({Id})";
    }

    #region Dispose

    private ImmutableArray<IProtocolPort> RemoveAllPorts()
    {
        lock (_sync)
        {
            var ports = _ports;
            _ports = [];
            return ports;
        }
    }

    private void DisposeObservables()
    {
        lock (_sync)
        {
            _portUpdated.Dispose();
            _portAdded.Dispose();
            _portRemoved.Dispose();
        }
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            if (disposing)
            {
                foreach (var port in RemoveAllPorts())
                {
                    _portRemoved.OnNext(port);
                    port.Dispose();
                }
                DisposeObservables();
                _logger.ZLogTrace($"{this} disposed");
            }
        }
        finally
        {
            base.Dispose(disposing);
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        try
        {
            foreach (var port in RemoveAllPorts())
            {
                _portRemoved.OnNext(port);
                await port.DisposeAsync().ConfigureAwait(false);
            }
            DisposeObservables();
        }
        finally
        {
            await base.DisposeAsyncCore().ConfigureAwait(false);
        }
    }

    #endregion
}
