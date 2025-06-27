using System;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;
using Microsoft.Extensions.Logging;
using R3;
using ZLogger;

namespace Asv.IO;

public abstract class ProtocolConnection : AsyncDisposableWithCancel, IProtocolConnection
{
    private ProtocolTags _internalTags = [];
    private readonly ILogger<ProtocolConnection> _logger;
    private readonly Subject<IProtocolMessage> _onTxMessage = new();
    private readonly Subject<IProtocolMessage> _onRxMessage = new();

    protected ProtocolConnection(string id, IProtocolContext context, IStatisticHandler? statistic = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(context);
        _logger = context.LoggerFactory.CreateLogger<ProtocolConnection>();
        if (statistic == null)
        {
            var value = new Statistic();
            Statistic = value;
            StatisticHandler = value;
        }
        else
        {
            var value = new InheritedStatistic(statistic);
            Statistic = value;
            StatisticHandler = value;
        }
        Id = id;
        Context = context;
        foreach (var feature in Context.Features)
        {
            feature.Register(this);
        }
    }

    public string Id { get; }
    public IStatistic Statistic { get; }
    public Observable<IProtocolMessage> OnTxMessage => _onTxMessage;
    public Observable<IProtocolMessage> OnRxMessage => _onRxMessage;
    protected IStatisticHandler StatisticHandler { get; }
    protected IProtocolContext Context { get; }
    public abstract ValueTask Send(IProtocolMessage message, CancellationToken cancel = default);
    public string? PrintMessage(IProtocolMessage message, PacketFormatting formatting = PacketFormatting.Inline) => Context.PrintMessage(message, formatting);

    protected async ValueTask<IProtocolMessage?> InternalFilterTxMessage(IProtocolMessage message)
    {
        foreach (var item in Context.Features)
        {
            var newMsg = await item.ProcessTx(message, this, DisposeCancel);
            if (newMsg == null) return null;
            message = newMsg;
        }
        return message;
    }
    protected async void InternalPublishRxMessage(IProtocolMessage message)
    {
        try
        {
            foreach (var item in Context.Features)
            {
                var newMsg = await item.ProcessRx(message, this, DisposeCancel);
                if (newMsg == null) return;
                message = newMsg;
            }
            _onRxMessage.OnNext(message);
        }
        catch (ProtocolConnectionException ex)
        {
            InternalPublishRxError(ex);
        }
        catch (Exception ex)
        {
            InternalPublishRxError(new ProtocolConnectionException(this,$"Error at publish rx message:{ex.Message}", ex));
        }
    }
    protected void InternalPublishRxError(Exception ex)
    {
        _onRxMessage.OnErrorResume(ex);
    }
    protected void InternalPublishTxError(ProtocolConnectionException ex)
    {
        _onTxMessage.OnErrorResume(ex);
    }
    protected void InternalPublishTxMessage(IProtocolMessage msg)
    {
        _onTxMessage.OnNext(msg);
    }
    public ref ProtocolTags Tags => ref _internalTags;
    
    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logger.ZLogTrace($"{nameof(Dispose)} connection {Id}");
            foreach (var feature in Context.Features)
            {
                feature.Unregister(this);
            }
            _onTxMessage.Dispose();
            _onRxMessage.Dispose();
        }
        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        _logger.ZLogTrace($"{nameof(DisposeAsyncCore)} port {this}");
        foreach (var feature in Context.Features)
        {
            feature.Unregister(this);
        }
        await CastAndDispose(_onTxMessage);
        await CastAndDispose(_onRxMessage);
        await base.DisposeAsyncCore();

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }

    #endregion
}