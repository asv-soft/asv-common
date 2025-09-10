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
            await port.Send(message, cancel);
        }
        InternalPublishTxMessage(message);
    }

    public Observable<IProtocolPort> PortUpdated => _portUpdated;

    public IProtocolPort AddPort(Uri connectionString)
    {
        if (!Context.PortFactory.TryGetValue(connectionString.Scheme, out var factory))
        {
            throw new InvalidOperationException($"Port scheme {connectionString.Scheme} not found");
        }
        var port = factory(connectionString, Context, StatisticHandler);

        // we don't need to dispose the port because it will be disposed when the router or port is disposed
        port.OnRxMessage.Subscribe(InternalPublishRxMessage);
        port.IsEnabled.Subscribe(port, (_, value) => _portUpdated.OnNext(value));

        ImmutableArray<IProtocolPort> after,
            before;
        do
        {
            before = _ports;
            after = before.Add(port);
        } while (
            ImmutableInterlocked.InterlockedCompareExchange(ref _ports, after, before) != before
        ); // check if the value is changed by another thread while we are adding the endpoint
        _logger.ZLogInformation($"{this} add {port}");
        if (port.Config.IsEnabled)
        {
            port.Enable();
        }
        _portAdded.OnNext(port);
        return port;
    }

    public void RemovePort(IProtocolPort port)
    {
        ImmutableArray<IProtocolPort> before,
            after;
        do
        {
            before = _ports;
            after = before.Remove(port);
        } while (
            ImmutableInterlocked.InterlockedCompareExchange(ref _ports, after, before) != before
        ); // check if the value is changed by another thread while we are removing the endpoint
        _portRemoved.OnNext(port);
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ImmutableArray<IProtocolPort> before,
                after;
            do
            {
                before = _ports;
                after = ImmutableArray<IProtocolPort>.Empty;
            } while (
                ImmutableInterlocked.InterlockedCompareExchange(ref _ports, after, before) != before
            ); // check if the value is changed by another thread while we are removing the endpoint

            foreach (var port in _ports)
            {
                _portRemoved.OnNext(port);
                port.Dispose();
            }
            _portUpdated.Dispose();
            _portAdded.Dispose();
            _portRemoved.Dispose();
            _logger.ZLogTrace($"{this} disposed");
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        ImmutableArray<IProtocolPort> before,
            after;
        do
        {
            before = _ports;
            after = ImmutableArray<IProtocolPort>.Empty;
        } while (
            ImmutableInterlocked.InterlockedCompareExchange(ref _ports, after, before) != before
        ); // check if the value is changed by another thread while we are removing the endpoint
        foreach (var port in _ports)
        {
            _portRemoved.OnNext(port);
            await port.DisposeAsync();
        }
        _portUpdated.Dispose();
        _portAdded.Dispose();
        _portRemoved.Dispose();
        await base.DisposeAsyncCore();
    }

    #endregion
}
