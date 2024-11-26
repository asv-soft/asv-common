using System;
using System.Linq;
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
    private readonly Subject<ProtocolConnectionException> _onError = new();

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

    public Observable<ProtocolConnectionException> OnError => _onError;

    protected IStatisticHandler StatisticHandler { get; }
    protected IProtocolContext Context { get; }
    public abstract ValueTask Send(IProtocolMessage message, CancellationToken cancel = default);
    public string? PrintMessage(IProtocolMessage message, PacketFormatting formatting = PacketFormatting.Inline)
    {
        return Context.Formatters
            .Where(x => x.CanPrint(message))
            .Select(x => x.Print(message, formatting))
            .FirstOrDefault();
    }

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
             InternalPublishError(ex);
        }
        catch (Exception ex)
        {
            InternalPublishError(new ProtocolConnectionException(this,$"Error at publish rx message:{ex.Message}", ex));
        }
    }
   
   
    protected void InternalPublishError(ProtocolConnectionException ex)
    {
        _onError.OnNext(ex);
    }
    public ref ProtocolTags Tags => ref _internalTags;
    
    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logger.ZLogTrace($"{nameof(Dispose)} port {this}");
            foreach (var feature in Context.Features)
            {
                feature.Unregister(this);
            }
            _onTxMessage.Dispose();
            _onRxMessage.Dispose();
            _onError.Dispose();
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
        await CastAndDispose(_onError);
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